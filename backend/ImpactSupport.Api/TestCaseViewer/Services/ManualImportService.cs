using System.Text;
using System.Text.Json;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class ManualImportService : IManualImportService
{
    private const string MasterKind = "master";
    private const string ResultKind = "result";
    private const string DryRunStatus = "DRY_RUN";
    private const string CommittedStatus = "COMMITTED";
    private const string NewStatus = "NEW";
    private const string ExistsStatus = "EXISTS";
    private const string OverwriteAction = "OVERWRITE";
    private const string SkipAction = "SKIP";
    private const string MasterFileId = "manual-master";

    private readonly SupportDbContext _dbContext;

    public ManualImportService(SupportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ImportBatchResponse> UploadMasterAsync(IFormFile file, AuthUser? user, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0) throw new ArgumentException("Upload file is empty.", nameof(file));

        var parsed = await ParseFilesAsync([file], MasterKind, cancellationToken);
        var dryRunErrors = parsed.Errors.ToList();
        var existingSheets = await _dbContext.QaDashboardSheetCaches
            .AsNoTracking()
            .Where(sheet => sheet.FileId == MasterFileId || sheet.FileCache!.ReportType == MasterKind)
            .Select(sheet => sheet.SheetName)
            .ToListAsync(cancellationToken);
        var existingSheetNames = existingSheets.Select(NormalizeKey).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var batch = new QaImportBatch
        {
            UploadKind = MasterKind,
            ResultMode = string.Empty,
            FileName = file.FileName,
            UploadedBy = user?.Username ?? string.Empty,
            Status = DryRunStatus,
            RowsError = parsed.Errors.Count
        };

        BuildBatch(batch, parsed.Rows, dryRunErrors, existingSheetNames);
        _dbContext.QaImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch, dryRunErrors);
    }

    public async Task<ImportBatchResponse> UploadResultsAsync(IReadOnlyList<IFormFile> files, string resultMode, AuthUser? user, CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) throw new ArgumentException("At least one result file is required.", nameof(files));

        var normalizedMode = resultMode.Equals("regression", StringComparison.OrdinalIgnoreCase) ? "regression" : "single";
        var parsed = await ParseFilesAsync(files, ResultKind, cancellationToken);
        var dryRunErrors = parsed.Errors.ToList();
        var batch = new QaImportBatch
        {
            UploadKind = ResultKind,
            ResultMode = normalizedMode,
            FileName = string.Join(", ", files.Select(file => file.FileName)),
            UploadedBy = user?.Username ?? string.Empty,
            Status = DryRunStatus,
            RowsError = parsed.Errors.Count
        };

        BuildBatch(batch, parsed.Rows, dryRunErrors, []);
        _dbContext.QaImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch, dryRunErrors);
    }

    public async Task<ImportBatchResponse?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        return batch == null ? null : ToResponse(batch);
    }

    public async Task<IReadOnlyList<ImportBatchErrorResponse>> GetErrorsAsync(int batchId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.QaImportBatchErrors
            .AsNoTracking()
            .Where(error => error.ImportBatchId == batchId)
            .OrderBy(error => error.RowNumber)
            .Select(error => new ImportBatchErrorResponse
            {
                Id = error.Id,
                RowNumber = error.RowNumber,
                RawValue = error.RawValue,
                ErrorMessage = error.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ImportBatchResponse?> SaveSheetActionsAsync(int batchId, SheetActionRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        if (batch == null || batch.Status == CommittedStatus) return batch == null ? null : ToResponse(batch);

        var actions = request.Actions.ToDictionary(item => item.SheetId, item => NormalizeAction(item.Action));
        foreach (var sheet in batch.Sheets.Where(sheet => sheet.ConflictStatus == ExistsStatus))
        {
            if (actions.TryGetValue(sheet.Id, out var action))
            {
                sheet.SelectedAction = action;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch);
    }

    public async Task<ImportBatchResponse?> CommitAsync(int batchId, CancellationToken cancellationToken = default)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        if (batch == null) return null;
        if (batch.Status == CommittedStatus) return ToResponse(batch);
        if (batch.RowsError > 0) throw new InvalidOperationException("Resolve validation errors before commit.");
        if (batch.UploadKind == MasterKind && batch.Sheets.Any(sheet => sheet.ConflictStatus == ExistsStatus && !IsFinalAction(sheet.SelectedAction)))
        {
            throw new InvalidOperationException("Choose overwrite or skip for every existing sheet/page before commit.");
        }

        var fileCache = await UpsertFileAsync(batch.UploadKind == ResultKind ? "regression" : MasterKind, GetFileId(batch), batch.FileName, cancellationToken);
        foreach (var stagedSheet in batch.Sheets)
        {
            var action = stagedSheet.ConflictStatus == NewStatus ? OverwriteAction : stagedSheet.SelectedAction;
            if (action == SkipAction)
            {
                batch.RowsSkipped += stagedSheet.RowCount;
                continue;
            }

            var sheet = UpsertSheet(fileCache, stagedSheet.SheetName);
            var rows = stagedSheet.Rows
                .OrderBy(row => row.SourceRowNumber)
                .Select(row => JsonSerializer.Deserialize<QaRow>(row.RowJson))
                .Where(row => row != null)
                .Cast<QaRow>()
                .ToList();
            ApplyRows(sheet, rows, stagedSheet.ModuleName);
            if (batch.UploadKind == MasterKind)
            {
                await ApplyMasterRowsAsync(rows, stagedSheet.SourceRowStart(), cancellationToken);
            }
            else
            {
                await ApplyTestingResultsAsync(batch, rows, cancellationToken);
            }
            stagedSheet.SelectedAction = stagedSheet.ConflictStatus == ExistsStatus ? OverwriteAction : stagedSheet.SelectedAction;
            stagedSheet.ConflictStatus = CommittedStatus;
        }

        fileCache.ScanStatus = "Success";
        fileCache.SyncStatus = "Local";
        fileCache.LastScannedAt = DateTimeOffset.UtcNow;
        fileCache.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
        batch.Status = CommittedStatus;
        batch.CommittedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch);
    }

    private async Task<ParsedUpload> ParseFilesAsync(IReadOnlyList<IFormFile> files, string uploadKind, CancellationToken cancellationToken)
    {
        var rows = new List<ParsedRow>();
        var errors = new List<QaImportBatchError>();

        foreach (var file in files)
        {
            var records = ParseDelimited(file.OpenReadStream(), DetectDelimiter(file.FileName));
            if (records.Count == 0) continue;

            var headerIndex = FindHeaderRow(records);
            if (headerIndex < 0)
            {
                errors.Add(new QaImportBatchError { RowNumber = 1, RawValue = file.FileName, ErrorMessage = "Header row with Test Case ID was not found." });
                continue;
            }

            var map = BuildHeaderMap(records[headerIndex]);
            for (var i = headerIndex + 1; i < records.Count; i++)
            {
                var record = records[i];
                if (record.All(string.IsNullOrWhiteSpace)) continue;

                var row = ToQaRow(file.FileName, record, map, uploadKind);
                var rowErrors = ValidateRow(row, record);
                if (rowErrors.Count > 0)
                {
                    errors.AddRange(rowErrors.Select(message => new QaImportBatchError
                    {
                        RowNumber = i + 1,
                        RawValue = string.Join(" | ", record),
                        ErrorMessage = message
                    }));
                    continue;
                }

                rows.Add(new ParsedRow(row, i + 1));
            }
        }

        return new ParsedUpload(rows, errors);
    }

    private static void BuildBatch(QaImportBatch batch, IReadOnlyList<ParsedRow> parsedRows, List<QaImportBatchError> errors, HashSet<string> existingSheetNames)
    {
        var duplicateIds = parsedRows
            .GroupBy(item => item.Row.TestCaseId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var duplicate in duplicateIds)
        {
            errors.Add(new QaImportBatchError
            {
                RowNumber = 0,
                RawValue = duplicate,
                ErrorMessage = "Duplicate Test Case ID appears more than once in this upload."
            });
        }

        foreach (var group in parsedRows.Where(item => !duplicateIds.Contains(item.Row.TestCaseId)).GroupBy(item => NormalizeKey(item.Row.SheetName)))
        {
            var first = group.First().Row;
            var exists = existingSheetNames.Contains(group.Key);
            var sheet = new QaImportBatchSheet
            {
                SheetName = first.SheetName,
                NormalizedSheetName = group.Key,
                ModuleName = first.Module,
                RowCount = group.Count(),
                ConflictStatus = exists ? ExistsStatus : NewStatus,
                SelectedAction = exists ? string.Empty : OverwriteAction
            };

            foreach (var parsed in group)
            {
                var stagedRow = new QaImportBatchRow
                {
                    ImportBatch = batch,
                    ImportBatchSheet = sheet,
                    SourceRowNumber = parsed.SourceRowNumber,
                    TestCaseId = parsed.Row.TestCaseId,
                    RowJson = JsonSerializer.Serialize(parsed.Row)
                };
                sheet.Rows.Add(stagedRow);
                batch.Rows.Add(stagedRow);
            }

            batch.Sheets.Add(sheet);
        }

        batch.SheetsDetected = batch.Sheets.Count;
        batch.NewSheets = batch.Sheets.Count(sheet => sheet.ConflictStatus == NewStatus);
        batch.ExistingSheets = batch.Sheets.Count(sheet => sheet.ConflictStatus == ExistsStatus);
        batch.RowsAdded = batch.Sheets.Where(sheet => sheet.ConflictStatus == NewStatus).Sum(sheet => sheet.RowCount);
        batch.RowsUpdated = batch.Sheets.Where(sheet => sheet.ConflictStatus == ExistsStatus).Sum(sheet => sheet.RowCount);
        batch.RowsError = errors.Count;
    }

    private static QaRow ToQaRow(string fileName, IReadOnlyList<string> record, IReadOnlyDictionary<string, List<int>> map, string uploadKind)
    {
        var sheetName = Path.GetFileNameWithoutExtension(fileName);

        return new QaRow
        {
            SourceFileId = uploadKind == ResultKind ? $"manual-result-{Path.GetFileNameWithoutExtension(fileName)}" : MasterFileId,
            SourceFileName = fileName,
            SheetName = sheetName,
            TestCaseNo = First(record, map, "Test Case No.", "TestCaseNo"),
            TestCaseId = First(record, map, "Test Case ID", "TestCaseId"),
            Preconditions = First(record, map, "Preconditions"),
            Module = First(record, map, "Module/Sub Module", "Module/ Sub Module", "Module"),
            PreparedBy = First(record, map, "Testcase Prepared Person Name", "PreparedBy"),
            PreparedDate = First(record, map, "Testcase Prepared Date", "PreparedDate"),
            TestingType = First(record, map, "Type of testing", "TestingType"),
            Description = First(record, map, "Test Case Description", "Description"),
            TestCases = First(record, map, "Test Cases", "TestCases"),
            TestData = First(record, map, "Test Data", "TestData"),
            ExpectedResult = First(record, map, "Expected Result", "ExpectedResult"),
            ActualResult = First(record, map, "Actual Result", "ActualResult"),
            IssueType = First(record, map, "Issue Type", "IssueType"),
            QaStatus = First(record, map, "QA Status", "QAStatus"),
            DevStatus = First(record, map, "Dev. Status", "DevStatus"),
            QaRemarks = GetRepeated(record, map, 4, "QA Remarks", "QARemarks"),
            DevRemarks = GetRepeated(record, map, 4, "Dev. Remarks", "DevRemarks"),
            Rounds = BuildRounds(record, map)
        };
    }

    private static List<string> ValidateRow(QaRow row, IReadOnlyList<string> record)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(row.TestCaseId)) errors.Add("Test Case ID is required.");
        if (string.IsNullOrWhiteSpace(row.SheetName)) errors.Add("Sheet Name is required.");
        if (string.IsNullOrWhiteSpace(row.Module)) errors.Add("Module/Sub Module is required.");
        if (!string.IsNullOrWhiteSpace(row.TestCaseId) && row.TestCaseId.Length < 3) errors.Add("Test Case ID is malformed.");
        if (!PreconditionIsRecognized(row.Preconditions)) errors.Add($"Unrecognized Preconditions value: '{row.Preconditions}'");
        foreach (var testingType in SplitTestingTypes(row.TestingType))
        {
            if (!KnownTestingTypes.Contains(testingType, StringComparer.OrdinalIgnoreCase)) errors.Add($"Unknown Type of testing value: '{testingType}'");
        }
        return errors;
    }

    private async Task<QaImportBatch?> LoadBatchAsync(int batchId, CancellationToken cancellationToken)
    {
        return await _dbContext.QaImportBatches
            .Include(batch => batch.Errors)
            .Include(batch => batch.Sheets)
            .ThenInclude(sheet => sheet.Rows)
            .FirstOrDefaultAsync(batch => batch.Id == batchId, cancellationToken);
    }

    private async Task<QaDashboardFileCache> UpsertFileAsync(string reportType, string fileId, string fileName, CancellationToken cancellationToken)
    {
        var file = await _dbContext.QaDashboardFileCaches
            .Include(item => item.Sheets)
            .FirstOrDefaultAsync(item => item.ReportType == reportType && item.FileId == fileId, cancellationToken);
        if (file != null)
        {
            file.FileName = string.IsNullOrWhiteSpace(fileName) ? file.FileName : fileName;
            return file;
        }

        file = new QaDashboardFileCache
        {
            FileId = fileId,
            FileName = string.IsNullOrWhiteSpace(fileName) ? "Manual Upload" : fileName,
            ReportType = reportType,
            SourceUrl = "manual-upload",
            ScanStatus = "Success",
            SyncStatus = "Local"
        };
        _dbContext.QaDashboardFileCaches.Add(file);
        return file;
    }

    private static QaDashboardSheetCache UpsertSheet(QaDashboardFileCache file, string sheetName)
    {
        var sheet = file.Sheets.FirstOrDefault(item => string.Equals(NormalizeKey(item.SheetName), NormalizeKey(sheetName), StringComparison.OrdinalIgnoreCase));
        if (sheet != null) return sheet;

        sheet = new QaDashboardSheetCache
        {
            FileId = file.FileId,
            SheetName = sheetName
        };
        file.Sheets.Add(sheet);
        return sheet;
    }

    private static void ApplyRows(QaDashboardSheetCache sheet, IReadOnlyList<QaRow> rows, string moduleName)
    {
        sheet.RowsJson = JsonSerializer.Serialize(rows);
        sheet.Module = rows.Select(row => row.Module).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? moduleName;
        sheet.PurposeOfTesting = sheet.Module;
        sheet.TotalTestCases = rows.Count;
        sheet.PassCount = CountByStatus(rows, "pass");
        sheet.FailedCount = CountByStatus(rows, "fail");
        sheet.FixedCount = CountByStatus(rows, "fixed");
        sheet.RejectedCount = CountByStatus(rows, "reject");
        sheet.PostponedCount = CountByStatus(rows, "postpon");
        sheet.WipCount = CountByStatus(rows, "wip");
        sheet.NotClearCount = CountByStatus(rows, "not clear", "clear");
        sheet.FutureDevelopmentCount = CountByStatus(rows, "future");
        sheet.DevStatus = rows.Select(row => row.DevStatus).LastOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        sheet.DevRemarks = string.Join("; ", rows.SelectMany(row => row.DevRemarks).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
        sheet.RefreshStatus = "Success";
        sheet.SyncStatus = "Local";
        sheet.LastRefreshedAt = DateTimeOffset.UtcNow;
        sheet.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
    }

    private async Task ApplyMasterRowsAsync(IReadOnlyList<QaRow> rows, int sourceRowStart, CancellationToken cancellationToken)
    {
        foreach (var row in rows)
        {
            var module = await GetOrCreateModuleAsync(row.Module, cancellationToken);
            var (roleId, clientId) = await ResolvePreconditionsAsync(row.Preconditions, cancellationToken);
            var master = await _dbContext.MasterTemplates
                .Include(item => item.Details)
                .Include(item => item.TestingTypes)
                .Include(item => item.Remarks)
                .FirstOrDefaultAsync(item => item.MasterTestId == row.TestCaseId, cancellationToken);
            if (master == null)
            {
                master = new MasterTemplate { MasterTestId = row.TestCaseId };
                _dbContext.MasterTemplates.Add(master);
            }

            master.MasterTestNo = row.TestCaseNo;
            master.MasterSourceSheet = row.SheetName;
            master.MasterSourceRow = sourceRowStart;
            master.MasterModules = module.Id;
            master.MasterPreconditionRole = roleId;
            master.MasterClient = clientId;
            master.MasterPreparedBy = row.PreparedBy;
            master.MasterPreparedDate = row.PreparedDate;
            master.MasterTestData = NormalizeText(row.TestData);
            master.MasterExpectedResult = NormalizeText(row.ExpectedResult);
            master.MasterActualResult = NormalizeText(row.ActualResult);
            master.MasterIssueType = await ResolveClosedLookupAsync(_dbContext.MasterIssueTypes, row.IssueType, cancellationToken);
            master.MasterQaStatus = await ResolveClosedLookupAsync(_dbContext.MasterQaStatuses, row.QaStatus, cancellationToken);
            master.MasterDevStatus = await ResolveClosedLookupAsync(_dbContext.MasterDevStatuses, row.DevStatus, cancellationToken);
            master.MasterIsCollaborative = false;
            master.MasterUpdatedAt = DateTimeOffset.UtcNow;
            master.Details ??= new MasterTestDetails { MasterTemplate = master };
            master.Details.MasterDescription = NormalizeText(row.Description);
            master.Details.MasterTestSteps = NormalizeText(row.TestCases);
            master.TestingTypes.Clear();
            foreach (var typeId in await ResolveTestingTypeIdsAsync(row.TestingType, cancellationToken))
            {
                master.TestingTypes.Add(new MasterTemplateTestingType { TestingTypeId = typeId });
            }

            master.Remarks.Clear();
            for (var i = 0; i < 4; i++)
            {
                var qa = i < row.QaRemarks.Count ? row.QaRemarks[i] : string.Empty;
                var dev = i < row.DevRemarks.Count ? row.DevRemarks[i] : string.Empty;
                if (string.IsNullOrWhiteSpace(qa) && string.IsNullOrWhiteSpace(dev)) continue;
                master.Remarks.Add(new MasterTemplateRemark { RoundNumber = i + 1, QaRemark = qa, DevRemark = dev });
            }
        }
    }

    private async Task ApplyTestingResultsAsync(QaImportBatch batch, IReadOnlyList<QaRow> rows, CancellationToken cancellationToken)
    {
        var meta = new TestingMetaResult { Name = batch.FileName, RunThrough = "MANUAL" };
        _dbContext.TestingMetaResults.Add(meta);
        var moduleStats = new Dictionary<int, TestingMetaResultModuleStat>();
        foreach (var row in rows)
        {
            var master = await _dbContext.MasterTemplates.AsNoTracking().FirstOrDefaultAsync(item => item.MasterTestId == row.TestCaseId, cancellationToken);
            if (master == null) continue;
            meta.DataResults.Add(new TestingDataResult
            {
                MasterTestId = row.TestCaseId,
                MasterIssueType = await ResolveClosedLookupAsync(_dbContext.MasterIssueTypes, row.IssueType, cancellationToken),
                MasterQaStatus = await ResolveClosedLookupAsync(_dbContext.MasterQaStatuses, row.QaStatus, cancellationToken),
                MasterDevStatus = await ResolveClosedLookupAsync(_dbContext.MasterDevStatuses, row.DevStatus, cancellationToken)
            });
            if (master.MasterModules is int moduleId)
            {
                if (!moduleStats.TryGetValue(moduleId, out var stat))
                {
                    stat = new TestingMetaResultModuleStat { MasterModuleId = moduleId };
                    moduleStats[moduleId] = stat;
                }
                if (row.QaStatus.Contains("pass", StringComparison.OrdinalIgnoreCase)) stat.PassCount++;
                if (row.QaStatus.Contains("fail", StringComparison.OrdinalIgnoreCase)) stat.FailCount++;
            }
        }

        foreach (var stat in moduleStats.Values) meta.ModuleStats.Add(stat);
    }

    private static ImportBatchResponse ToResponse(QaImportBatch batch, IReadOnlyList<QaImportBatchError>? transientErrors = null)
    {
        return new ImportBatchResponse
        {
            BatchId = batch.Id,
            UploadKind = batch.UploadKind,
            ResultMode = batch.ResultMode,
            FileName = batch.FileName,
            Status = batch.Status,
            RowsAdded = batch.RowsAdded,
            RowsUpdated = batch.RowsUpdated,
            RowsSkipped = batch.RowsSkipped,
            RowsError = batch.RowsError,
            SheetsDetected = batch.SheetsDetected,
            NewSheets = batch.NewSheets,
            ExistingSheets = batch.ExistingSheets,
            Errors = (transientErrors ?? batch.Errors).Select(error => new ImportBatchErrorResponse { Id = error.Id, RowNumber = error.RowNumber, RawValue = error.RawValue, ErrorMessage = error.ErrorMessage }).ToList(),
            Sheets = batch.Sheets.OrderBy(sheet => sheet.SheetName).Select(sheet => new ImportBatchSheetResponse
            {
                Id = sheet.Id,
                SheetName = sheet.SheetName,
                ModuleName = sheet.ModuleName,
                RowCount = sheet.RowCount,
                ConflictStatus = sheet.ConflictStatus,
                SelectedAction = sheet.SelectedAction
            }).ToList()
        };
    }

    private static List<List<string>> ParseDelimited(Stream stream, string delimiter)
    {
        var rows = new List<List<string>>();
        using var parser = new TextFieldParser(stream, Encoding.UTF8, detectEncoding: true);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(delimiter);
        parser.HasFieldsEnclosedInQuotes = true;
        while (!parser.EndOfData)
        {
            rows.Add((parser.ReadFields() ?? []).Select(field => field.Trim()).ToList());
        }

        return rows;
    }

    private static string DetectDelimiter(string fileName)
    {
        return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";
    }

    private static int FindHeaderRow(IReadOnlyList<IReadOnlyList<string>> records)
    {
        for (var i = 0; i < records.Count; i++)
        {
            if (records[i].Any(cell => cell.Equals("Test Case ID", StringComparison.OrdinalIgnoreCase) || cell.Equals("TestCaseId", StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, List<int>> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            var key = header[i].Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!map.TryGetValue(key, out var indexes))
            {
                indexes = [];
                map[key] = indexes;
            }

            indexes.Add(i);
        }

        return map;
    }

    private static string First(IReadOnlyList<string> row, IReadOnlyDictionary<string, List<int>> map, params string[] headers)
    {
        foreach (var header in headers)
        {
            var value = Get(row, map, header);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return string.Empty;
    }

    private static string Get(IReadOnlyList<string> row, IReadOnlyDictionary<string, List<int>> map, string header)
    {
        return map.TryGetValue(header, out var indexes) && indexes.Count > 0 && indexes[0] < row.Count ? row[indexes[0]].Trim() : string.Empty;
    }

    private static List<string> GetRepeated(IReadOnlyList<string> row, IReadOnlyDictionary<string, List<int>> map, int maxCount, params string[] headers)
    {
        return headers
            .Where(map.ContainsKey)
            .SelectMany(header => map[header])
            .Take(maxCount)
            .Where(index => index < row.Count)
            .Select(index => row[index].Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static List<QaRound> BuildRounds(IReadOnlyList<string> row, IReadOnlyDictionary<string, List<int>> map)
    {
        var qaIndexes = map.TryGetValue("QA Status", out var qa) ? qa : [];
        var devIndexes = map.TryGetValue("Dev. Status", out var dev) ? dev : [];
        var max = Math.Max(qaIndexes.Count, devIndexes.Count);
        max = Math.Min(max, 4);
        var rounds = new List<QaRound>();
        for (var i = 0; i < max; i++)
        {
            var qaStatus = i < qaIndexes.Count && qaIndexes[i] < row.Count ? row[qaIndexes[i]].Trim() : string.Empty;
            var devStatus = i < devIndexes.Count && devIndexes[i] < row.Count ? row[devIndexes[i]].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(qaStatus) && string.IsNullOrWhiteSpace(devStatus)) continue;
            rounds.Add(new QaRound { RoundNumber = i + 1, QaStatus = qaStatus, DevStatus = devStatus });
        }

        return rounds;
    }

    private static int CountByStatus(IEnumerable<QaRow> rows, params string[] terms)
    {
        return rows.Count(row =>
        {
            var value = $"{row.QaStatus} {row.DevStatus} {row.IssueType} {row.ActualResult}".ToLowerInvariant();
            return terms.Any(value.Contains);
        });
    }

    private static string GetFileId(QaImportBatch batch)
    {
        return batch.UploadKind == ResultKind
            ? $"manual-results-{batch.Id}-{NormalizeKey(batch.ResultMode)}"
            : MasterFileId;
    }

    private static string NormalizeKey(string value)
    {
        return string.Join(" ", value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeAction(string action)
    {
        return action.Equals(SkipAction, StringComparison.OrdinalIgnoreCase) ? SkipAction : OverwriteAction;
    }

    private static bool IsFinalAction(string action)
    {
        return action == OverwriteAction || action == SkipAction;
    }

    private static bool PreconditionIsRecognized(string value)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return true;
        if (trimmed.Equals("All user", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("All roles", StringComparison.OrdinalIgnoreCase)) return true;
        var roleText = trimmed;
        var paren = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(.*?)\s*\(([A-Za-z0-9]+)\)\s*$");
        if (paren.Success)
        {
            roleText = paren.Groups[1].Value;
        }
        else if (trimmed.Contains('_'))
        {
            roleText = trimmed.Split('_', 2)[0];
        }

        roleText = System.Text.RegularExpressions.Regex.Replace(roleText, @"\s*role\s*$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        return new[] { "Author", "PE", "Collator", "Editor" }.Any(role => role.Equals(roleText, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<MasterModule> GetOrCreateModuleAsync(string value, CancellationToken cancellationToken)
    {
        var normalized = value.Trim();
        var module = await _dbContext.MasterModules.FirstOrDefaultAsync(item => item.Name == normalized, cancellationToken);
        if (module != null) return module;
        module = new MasterModule { Name = normalized };
        _dbContext.MasterModules.Add(module);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return module;
    }

    private async Task<(int? RoleId, int? ClientId)> ResolvePreconditionsAsync(string value, CancellationToken cancellationToken)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("All user", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("All roles", StringComparison.OrdinalIgnoreCase)) return (null, null);
        var role = trimmed;
        string client = string.Empty;
        var paren = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(.*?)\s*\(([A-Za-z0-9]+)\)\s*$");
        if (paren.Success) { role = paren.Groups[1].Value; client = paren.Groups[2].Value; }
        else if (trimmed.Contains('_')) { var parts = trimmed.Split('_', 2); role = parts[0]; client = parts[1]; }
        role = System.Text.RegularExpressions.Regex.Replace(role, @"\s*role\s*$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        var roleId = await ResolveClosedLookupAsync(_dbContext.MasterPreconditionRoles, role, cancellationToken);
        int? clientId = null;
        if (!string.IsNullOrWhiteSpace(client))
        {
            var code = client.Trim().ToUpperInvariant();
            var row = await _dbContext.Clients.FirstOrDefaultAsync(item => item.Code == code, cancellationToken);
            if (row == null)
            {
                row = new Client { Code = code, Name = code };
                _dbContext.Clients.Add(row);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            clientId = row.Id;
        }
        return (roleId, clientId);
    }

    private static async Task<int?> ResolveClosedLookupAsync<T>(DbSet<T> set, string value, CancellationToken cancellationToken) where T : class
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var property = typeof(T).GetProperty("Value") ?? typeof(T).GetProperty("Code") ?? typeof(T).GetProperty("Name");
        var rows = await set.AsNoTracking().ToListAsync(cancellationToken);
        var row = rows.FirstOrDefault(item => string.Equals(property?.GetValue(item)?.ToString(), value.Trim(), StringComparison.OrdinalIgnoreCase));
        return row == null ? null : (int?)typeof(T).GetProperty("Id")!.GetValue(row);
    }

    private async Task<IReadOnlyList<int>> ResolveTestingTypeIdsAsync(string value, CancellationToken cancellationToken)
    {
        var ids = new List<int>();
        foreach (var type in SplitTestingTypes(value))
        {
            var id = await ResolveClosedLookupAsync(_dbContext.MasterTestingTypes, type, cancellationToken);
            if (id.HasValue) ids.Add(id.Value);
        }
        return ids;
    }

    private static IReadOnlyList<string> SplitTestingTypes(string value) => value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string NormalizeText(string value) => value.Replace("\r\n", "\n").Replace('\r', '\n');
    private static readonly string[] KnownTestingTypes = ["Basic", "Mock", "Browser", "Regression", "Tomcat_Reg"];

    private sealed record ParsedUpload(IReadOnlyList<ParsedRow> Rows, IReadOnlyList<QaImportBatchError> Errors);
    private sealed record ParsedRow(QaRow Row, int SourceRowNumber);
}

file static class ImportSheetExtensions
{
    public static int SourceRowStart(this QaImportBatchSheet sheet) => sheet.Rows.OrderBy(row => row.SourceRowNumber).FirstOrDefault()?.SourceRowNumber ?? 0;
}
