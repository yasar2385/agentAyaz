using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using System.IO.Compression;
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
    private const string SkipRowAction = "SKIP_ROW";
    private const string MasterFileId = "manual-master";
    private static readonly TimeSpan UploadTokenTtl = TimeSpan.FromMinutes(30);

    private readonly SupportDbContext _dbContext;

    public ManualImportService(SupportDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ImportInspectResponse> InspectAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0) throw new ArgumentException("Upload file is empty.", nameof(file));
        CleanupExpiredUploads();
        var token = Guid.NewGuid().ToString("N");
        var directory = TempUploadDirectory();
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(file.FileName);
        var dataPath = Path.Combine(directory, $"{token}{extension}");
        await using (var output = File.Create(dataPath))
        await using (var input = file.OpenReadStream())
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        var metadata = new TempUploadMetadata
        {
            Token = token,
            FileName = file.FileName,
            StoredPath = dataPath,
            ExpiresAt = DateTimeOffset.UtcNow.Add(UploadTokenTtl)
        };
        await File.WriteAllTextAsync(MetadataPath(token), JsonSerializer.Serialize(metadata), cancellationToken);

        var sheets = file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? InspectXlsx(dataPath)
            : [new ImportInspectSheetResponse { SheetName = Path.GetFileNameWithoutExtension(file.FileName), Visibility = "visible", RowCountEstimate = 0 }];

        return new ImportInspectResponse
        {
            UploadToken = token,
            SourceType = SourceType(file.FileName),
            Sheets = sheets
        };
    }

    public async Task<ImportBatchResponse> ParseMasterAsync(ParseMasterImportRequest request, AuthUser? user, CancellationToken cancellationToken = default)
    {
        var metadata = await ReadTempUploadAsync(request.UploadToken, cancellationToken);
        if (metadata == null) throw new ArgumentException("Upload token is missing or expired.");
        if (metadata.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) && request.SheetNames.Count == 0)
        {
            throw new ArgumentException("Select at least one workbook sheet before dry-run.");
        }

        await using var stream = new FileStream(metadata.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var file = new FormFile(stream, 0, stream.Length, "file", metadata.FileName);
        try
        {
            return await UploadMasterAsync(file, user, cancellationToken, request.SheetNames);
        }
        finally
        {
            DeleteTempUpload(metadata);
        }
    }

    public async Task<ImportBatchResponse> UploadMasterAsync(IFormFile file, AuthUser? user, CancellationToken cancellationToken = default)
        => await UploadMasterAsync(file, user, cancellationToken, []);

    private async Task<ImportBatchResponse> UploadMasterAsync(IFormFile file, AuthUser? user, CancellationToken cancellationToken, IReadOnlyList<string> selectedSheetNames)
    {
        if (file.Length == 0) throw new ArgumentException("Upload file is empty.", nameof(file));

        var parsed = await ParseFilesAsync([file], MasterKind, cancellationToken, selectedSheetNames);
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

        var existingTestCaseIds = await _dbContext.MasterTemplates.AsNoTracking().Select(item => item.MasterTestId).ToListAsync(cancellationToken);
        BuildBatch(batch, parsed.Rows, dryRunErrors, existingSheetNames, resolveDuplicateIds: true, existingTestCaseIds);
        await MarkManualEditConflictsAsync(batch, cancellationToken);
        _dbContext.QaImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch, dryRunErrors);
    }

    public async Task<ImportBatchResponse> UploadResultsAsync(IReadOnlyList<IFormFile> files, string resultMode, AuthUser? user, CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) throw new ArgumentException("At least one result file is required.", nameof(files));

        var normalizedMode = resultMode.Equals("regression", StringComparison.OrdinalIgnoreCase) ? "regression" : "single";
        var parsed = await ParseFilesAsync(files, ResultKind, cancellationToken, []);
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

        BuildBatch(batch, parsed.Rows, dryRunErrors, [], resolveDuplicateIds: false, []);
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

    public async Task<ImportBatchResponse?> SaveManualEditActionsAsync(int batchId, ManualEditActionRequest request, CancellationToken cancellationToken = default)
    {
        var batch = await LoadBatchAsync(batchId, cancellationToken);
        if (batch == null || batch.Status == CommittedStatus) return batch == null ? null : ToResponse(batch);

        var actions = request.Actions.ToDictionary(item => item.RowId, item => NormalizeManualEditAction(item.Action));
        foreach (var row in batch.Rows.Where(row => row.ManualEditConflict))
        {
            if (actions.TryGetValue(row.Id, out var action))
            {
                row.ManualEditAction = action;
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
        if (batch.UploadKind == MasterKind && batch.Rows.Any(row => row.ManualEditConflict && !IsFinalManualEditAction(row.ManualEditAction)))
        {
            throw new InvalidOperationException("Choose overwrite or skip row for every manually edited test case before commit.");
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
            var skippedRows = stagedSheet.Rows.Count(row => row.ManualEditConflict && row.ManualEditAction == SkipRowAction);
            batch.RowsSkipped += skippedRows;
            var rows = stagedSheet.Rows
                .Where(row => !(row.ManualEditConflict && row.ManualEditAction == SkipRowAction))
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

    private async Task<ParsedUpload> ParseFilesAsync(IReadOnlyList<IFormFile> files, string uploadKind, CancellationToken cancellationToken, IReadOnlyList<string> selectedSheetNames)
    {
        var rows = new List<ParsedRow>();
        var errors = new List<QaImportBatchError>();

        foreach (var file in files)
        {
            var selected = selectedSheetNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceSheet in ParseSourceSheets(file, selected))
            {
                var records = sourceSheet.Records;
                if (records.Count == 0) continue;

                var headerIndex = FindHeaderRow(records);
                if (headerIndex < 0)
                {
                    errors.Add(new QaImportBatchError { RowNumber = 1, RawValue = sourceSheet.SourceName, ErrorMessage = "Header row with Test Case ID was not found." });
                    continue;
                }

                var map = BuildHeaderMap(records[headerIndex]);
                for (var i = headerIndex + 1; i < records.Count; i++)
                {
                    var record = records[i];
                    if (record.All(string.IsNullOrWhiteSpace)) continue;

                    var row = ToQaRow(file.FileName, sourceSheet.SourceName, record, map, uploadKind);
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
        }

        return new ParsedUpload(rows, errors);
    }

    private static void BuildBatch(
        QaImportBatch batch,
        IReadOnlyList<ParsedRow> parsedRows,
        List<QaImportBatchError> errors,
        HashSet<string> existingSheetNames,
        bool resolveDuplicateIds,
        IReadOnlyCollection<string> existingTestCaseIds)
    {
        var duplicateIds = resolveDuplicateIds
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : parsedRows
            .GroupBy(item => item.Row.TestCaseId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (resolveDuplicateIds)
        {
            ResolveDuplicateIds(parsedRows, existingTestCaseIds);
        }
        else
        {
            foreach (var duplicate in duplicateIds)
            {
                errors.Add(new QaImportBatchError
                {
                    RowNumber = 0,
                    RawValue = duplicate,
                    ErrorMessage = "Duplicate Test Case ID appears more than once in this upload."
                });
            }
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
                    OriginalRawTestCaseId = parsed.Row.OriginalRawTestCaseId,
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

    private static void ResolveDuplicateIds(IReadOnlyList<ParsedRow> parsedRows, IReadOnlyCollection<string> existingTestCaseIds)
    {
        var usedIds = new HashSet<string>(existingTestCaseIds, StringComparer.OrdinalIgnoreCase);
        foreach (var parsed in parsedRows)
        {
            usedIds.Add(parsed.Row.TestCaseId.Trim());
        }

        var duplicateGroups = parsedRows
            .GroupBy(item => item.Row.TestCaseId.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            var occurrences = group.ToList();
            var baseId = occurrences[0].Row.TestCaseId.Trim();
            occurrences[0].Row.TestCaseId = baseId;
            occurrences[0].Row.OriginalRawTestCaseId = null;

            var suffixIndex = 1;
            for (var i = 1; i < occurrences.Count; i++)
            {
                var rawId = occurrences[i].Row.TestCaseId.Trim();
                usedIds.Remove(rawId);
                var resolvedId = NextAvailableSuffixedId(baseId, usedIds, ref suffixIndex);
                occurrences[i].Row.TestCaseId = resolvedId;
                occurrences[i].Row.OriginalRawTestCaseId = rawId;
                usedIds.Add(resolvedId);
            }
        }
    }

    private async Task MarkManualEditConflictsAsync(QaImportBatch batch, CancellationToken cancellationToken)
    {
        var ids = batch.Rows.Select(row => row.TestCaseId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0) return;

        var editedRows = await _dbContext.MasterTemplates
            .AsNoTracking()
            .Where(master => ids.Contains(master.MasterTestId) && !string.IsNullOrWhiteSpace(master.MasterUpdatedBy))
            .Select(master => new { master.MasterTestId, master.MasterUpdatedBy, master.MasterUpdatedAt })
            .ToListAsync(cancellationToken);
        var editedById = editedRows.ToDictionary(item => item.MasterTestId, StringComparer.OrdinalIgnoreCase);
        foreach (var row in batch.Rows)
        {
            if (!editedById.TryGetValue(row.TestCaseId, out var existing)) continue;
            row.ManualEditConflict = true;
            row.ManualEditAction = string.Empty;
            row.ManualEditLastEditedBy = existing.MasterUpdatedBy ?? string.Empty;
            row.ManualEditLastEditedAt = existing.MasterUpdatedAt;
        }
    }

    private static string NextAvailableSuffixedId(string baseId, HashSet<string> usedIds, ref int suffixIndex)
    {
        while (true)
        {
            var candidate = baseId + SuffixFor(suffixIndex++);
            if (!usedIds.Contains(candidate)) return candidate;
        }
    }

    private static string SuffixFor(int index)
    {
        var value = index;
        var suffix = string.Empty;
        while (value > 0)
        {
            value--;
            suffix = (char)('a' + value % 26) + suffix;
            value /= 26;
        }
        return suffix;
    }

    private static QaRow ToQaRow(string fileName, string sourceSheetName, IReadOnlyList<string> record, IReadOnlyDictionary<string, List<int>> map, string uploadKind)
    {
        var sheetName = sourceSheetName;

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
            TestData = NormalizeText(First(record, map, "Test Data", "TestData")),
            ExpectedResult = NormalizeText(First(record, map, "Expected Result", "ExpectedResult")),
            ActualResult = NormalizeText(First(record, map, "Actual Result", "ActualResult")),
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
            if (!KnownTestingTypes.Contains(NormalizeTestingType(testingType), StringComparer.OrdinalIgnoreCase)) errors.Add($"Unknown Type of testing value: '{testingType}'");
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
            var precondition = await ResolvePreconditionsAsync(row.Preconditions, cancellationToken);
            var master = await _dbContext.MasterTemplates
                .Include(item => item.Details)
                .Include(item => item.TestingTypes)
                .Include(item => item.Remarks)
                .Include(item => item.Clients)
                .FirstOrDefaultAsync(item => item.MasterTestId == row.TestCaseId, cancellationToken);
            if (master == null)
            {
                master = new MasterTemplate { MasterTestId = row.TestCaseId };
                _dbContext.MasterTemplates.Add(master);
            }

            master.MasterTestNo = row.TestCaseNo;
            master.MasterOriginalRawId = string.IsNullOrWhiteSpace(row.OriginalRawTestCaseId) ? null : row.OriginalRawTestCaseId;
            master.MasterSourceSheet = row.SheetName;
            master.MasterSourceRow = sourceRowStart;
            master.MasterModules = module.Id;
            master.MasterPreconditionRole = precondition.RoleId;
            master.MasterClient = precondition.ClientIds.Count > 0 ? precondition.ClientIds[0] : null;
            master.MasterType = precondition.MasterTypeId;
            master.MasterPreparedBy = row.PreparedBy;
            master.MasterPreparedDate = row.PreparedDate;
            master.MasterTestData = NormalizeText(row.TestData);
            master.MasterExpectedResult = NormalizeText(row.ExpectedResult);
            master.MasterActualResult = NormalizeText(row.ActualResult);
            master.MasterIssueType = await ResolveClosedLookupAsync(_dbContext.MasterIssueTypes, row.IssueType, cancellationToken);
            master.MasterQaStatus = await ResolveClosedLookupAsync(_dbContext.MasterQaStatuses, row.QaStatus, cancellationToken);
            master.MasterDevStatus = await ResolveClosedLookupAsync(_dbContext.MasterDevStatuses, row.DevStatus, cancellationToken);
            master.MasterIsSharedRole = precondition.IsSharedRole;
            master.MasterIsCollaborative = precondition.IsBook && precondition.ClientCodes.Any(code => code is "OSO" or "OXMEDO");
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

            master.Clients.Clear();
            foreach (var clientId in precondition.ClientIds.Distinct())
            {
                master.Clients.Add(new MasterTemplateClient { ClientId = clientId });
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
            SourceType = SourceType(batch.FileName),
            Status = batch.Status,
            RowsAdded = batch.RowsAdded,
            RowsUpdated = batch.RowsUpdated,
            RowsSkipped = batch.RowsSkipped,
            RowsError = batch.RowsError,
            SheetsDetected = batch.SheetsDetected,
            NewSheets = batch.NewSheets,
            ExistingSheets = batch.ExistingSheets,
            Errors = (transientErrors ?? batch.Errors).Select(error => new ImportBatchErrorResponse { Id = error.Id, RowNumber = error.RowNumber, RawValue = error.RawValue, ErrorMessage = error.ErrorMessage }).ToList(),
            DuplicateIdsResolved = DuplicateIdResponses(batch),
            ManualEditConflicts = batch.Rows
                .Where(row => row.ManualEditConflict)
                .OrderBy(row => row.ImportBatchSheet?.SheetName)
                .ThenBy(row => row.SourceRowNumber)
                .Select(row => new ManualEditConflictResponse
                {
                    RowId = row.Id,
                    MasterTestId = row.TestCaseId,
                    SheetName = row.ImportBatchSheet?.SheetName ?? string.Empty,
                    SourceRowNumber = row.SourceRowNumber,
                    LastEditedBy = row.ManualEditLastEditedBy,
                    LastEditedAt = row.ManualEditLastEditedAt,
                    SelectedAction = row.ManualEditAction
                })
                .ToList(),
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

    private static IReadOnlyList<DuplicateIdResolutionResponse> DuplicateIdResponses(QaImportBatch batch)
    {
        var duplicateRawIds = batch.Rows
            .Where(row => !string.IsNullOrWhiteSpace(row.OriginalRawTestCaseId))
            .Select(row => row.OriginalRawTestCaseId!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (duplicateRawIds.Count == 0) return [];

        var responses = new List<DuplicateIdResolutionResponse>();
        foreach (var rawId in duplicateRawIds)
        {
            responses.AddRange(batch.Rows
                .Where(row => string.Equals(row.TestCaseId, rawId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.OriginalRawTestCaseId, rawId, StringComparison.OrdinalIgnoreCase))
                .Select(row => new DuplicateIdResolutionResponse
                {
                    RawId = row.OriginalRawTestCaseId ?? row.TestCaseId,
                    ResolvedId = row.TestCaseId,
                    SheetName = row.ImportBatchSheet?.SheetName ?? string.Empty,
                    SourceRowNumber = row.SourceRowNumber
                }));
        }

        return responses
            .DistinctBy(item => (item.RawId.ToUpperInvariant(), item.ResolvedId.ToUpperInvariant(), item.SheetName, item.SourceRowNumber))
            .OrderBy(item => item.SheetName)
            .ThenBy(item => item.SourceRowNumber)
            .ToList();
    }

    private static IReadOnlyList<ParsedSheet> ParseSourceSheets(IFormFile file, HashSet<string> selectedSheetNames)
    {
        var sourceName = Path.GetFileNameWithoutExtension(file.FileName);
        if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return ParseXlsx(file.OpenReadStream(), selectedSheetNames);
        }

        return [new ParsedSheet(sourceName, ParseDelimited(file.OpenReadStream(), DetectDelimiter(file.FileName)))];
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
            rows.Add((parser.ReadFields() ?? []).Select(field => NormalizeCell(field)).ToList());
        }

        return rows;
    }

    private static IReadOnlyList<ParsedSheet> ParseXlsx(Stream stream, HashSet<string> selectedSheetNames)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var sharedStrings = ReadSharedStrings(archive);
        var workbook = XDocument.Load(archive.GetEntry("xl/workbook.xml")!.Open());
        var rels = XDocument.Load(archive.GetEntry("xl/_rels/workbook.xml.rels")!.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relTargets = rels.Root!.Elements(packageRelNs + "Relationship")
            .ToDictionary(item => item.Attribute("Id")!.Value, item => item.Attribute("Target")!.Value);
        var sheets = new List<ParsedSheet>();
        foreach (var sheet in workbook.Root!.Element(main + "sheets")!.Elements(main + "sheet"))
        {
            var name = sheet.Attribute("name")!.Value;
            if (selectedSheetNames.Count > 0 && !selectedSheetNames.Contains(name)) continue;
            var relationshipId = sheet.Attribute(relNs + "id")!.Value;
            if (!relTargets.TryGetValue(relationshipId, out var target)) continue;
            var entryPath = "xl/" + target.TrimStart('/').Replace('\\', '/');
            var entry = archive.GetEntry(entryPath);
            if (entry == null) continue;
            sheets.Add(new ParsedSheet(name, ReadWorksheet(entry, sharedStrings)));
        }

        return sheets;
    }

    private static IReadOnlyList<ImportInspectSheetResponse> InspectXlsx(string path)
    {
        using var stream = File.OpenRead(path);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var workbook = XDocument.Load(archive.GetEntry("xl/workbook.xml")!.Open());
        var rels = XDocument.Load(archive.GetEntry("xl/_rels/workbook.xml.rels")!.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace packageRelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relTargets = rels.Root!.Elements(packageRelNs + "Relationship")
            .ToDictionary(item => item.Attribute("Id")!.Value, item => item.Attribute("Target")!.Value);
        var result = new List<ImportInspectSheetResponse>();
        foreach (var sheet in workbook.Root!.Element(main + "sheets")!.Elements(main + "sheet"))
        {
            var name = sheet.Attribute("name")!.Value;
            var visibility = (sheet.Attribute("state")?.Value ?? "visible") switch
            {
                "hidden" => "hidden",
                "veryHidden" => "very_hidden",
                "very_hidden" => "very_hidden",
                _ => "visible"
            };
            var relationshipId = sheet.Attribute(relNs + "id")!.Value;
            var rowCount = 0;
            if (relTargets.TryGetValue(relationshipId, out var target))
            {
                var entry = archive.GetEntry("xl/" + target.TrimStart('/').Replace('\\', '/'));
                if (entry != null)
                {
                    rowCount = EstimateRows(entry);
                }
            }
            result.Add(new ImportInspectSheetResponse { SheetName = name, Visibility = visibility, RowCountEstimate = rowCount });
        }

        return result;
    }

    private static int EstimateRows(ZipArchiveEntry entry)
    {
        var document = XDocument.Load(entry.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Descendants(main + "row").Count();
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return [];
        var document = XDocument.Load(entry.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return document.Root!.Elements(main + "si")
            .Select(item => string.Concat(item.Descendants(main + "t").Select(text => text.Value)))
            .Select(NormalizeCell)
            .ToList();
    }

    private static List<List<string>> ReadWorksheet(ZipArchiveEntry entry, IReadOnlyList<string> sharedStrings)
    {
        var document = XDocument.Load(entry.Open());
        XNamespace main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = new List<List<string>>();
        foreach (var row in document.Descendants(main + "row"))
        {
            var cells = new SortedDictionary<int, string>();
            foreach (var cell in row.Elements(main + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? string.Empty;
                var index = ColumnIndex(reference);
                if (index < 0) continue;
                cells[index] = ReadCell(cell, sharedStrings, main);
            }

            if (cells.Count == 0)
            {
                rows.Add([]);
                continue;
            }

            var width = cells.Keys.Max() + 1;
            var values = Enumerable.Repeat(string.Empty, width).ToList();
            foreach (var item in cells) values[item.Key] = item.Value;
            rows.Add(values);
        }

        return rows;
    }

    private static string ReadCell(XElement cell, IReadOnlyList<string> sharedStrings, XNamespace main)
    {
        var type = cell.Attribute("t")?.Value ?? string.Empty;
        if (type == "inlineStr")
        {
            return NormalizeCell(string.Concat(cell.Descendants(main + "t").Select(text => text.Value)));
        }

        var value = cell.Element(main + "v")?.Value ?? string.Empty;
        if (type == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count)
        {
            return sharedStrings[index];
        }

        return NormalizeCell(value);
    }

    private static int ColumnIndex(string reference)
    {
        var letters = new string(reference.TakeWhile(char.IsLetter).ToArray());
        if (letters.Length == 0) return -1;
        var index = 0;
        foreach (var letter in letters.ToUpperInvariant())
        {
            index = index * 26 + letter - 'A' + 1;
        }

        return index - 1;
    }

    private static string DetectDelimiter(string fileName)
    {
        return fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";
    }

    private static string SourceType(string fileName)
    {
        if (fileName.Split(',', StringSplitOptions.TrimEntries).Any(name => name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))) return "XLSX workbook";
        if (fileName.Split(',', StringSplitOptions.TrimEntries).Any(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))) return "CSV";
        return "TSV";
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

    private static string NormalizeManualEditAction(string action)
    {
        if (action.Equals(OverwriteAction, StringComparison.OrdinalIgnoreCase)) return OverwriteAction;
        if (action.Equals(SkipRowAction, StringComparison.OrdinalIgnoreCase)) return SkipRowAction;
        return string.Empty;
    }

    private static bool IsFinalAction(string action)
    {
        return action == OverwriteAction || action == SkipAction;
    }

    private static bool IsFinalManualEditAction(string action)
    {
        return action == OverwriteAction || action == SkipRowAction;
    }

    private static bool PreconditionIsRecognized(string value)
    {
        return TryParsePrecondition(value, out var parsed) && (string.IsNullOrWhiteSpace(parsed.Role) || KnownRoles.Contains(parsed.Role, StringComparer.OrdinalIgnoreCase));
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

    private async Task<ResolvedPrecondition> ResolvePreconditionsAsync(string value, CancellationToken cancellationToken)
    {
        if (!TryParsePrecondition(value, out var parsed))
        {
            return new ResolvedPrecondition(null, [], [], false, false, null);
        }

        var roleId = string.IsNullOrWhiteSpace(parsed.Role)
            ? null
            : await ResolveClosedLookupAsync(_dbContext.MasterPreconditionRoles, parsed.Role, cancellationToken);
        var clientIds = new List<int>();
        var clientCodes = new List<string>();
        foreach (var token in parsed.ClientTokens)
        {
            var client = await ResolveClientAsync(token, cancellationToken);
            clientIds.Add(client.Id);
            clientCodes.Add(client.Code);
        }

        var typeId = parsed.IsBook ? await ResolveClosedLookupAsync(_dbContext.Types, "Book", cancellationToken) : null;
        return new ResolvedPrecondition(roleId, clientIds, clientCodes, parsed.IsSharedRole, parsed.IsBook, typeId);
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
            var id = await ResolveTestingTypeIdAsync(type, cancellationToken);
            if (id.HasValue) ids.Add(id.Value);
        }
        return ids;
    }

    private async Task<int?> ResolveTestingTypeIdAsync(string value, CancellationToken cancellationToken)
    {
        var normalized = NormalizeTestingType(value);
        var id = await ResolveClosedLookupAsync(_dbContext.MasterTestingTypes, normalized, cancellationToken);
        if (id.HasValue) return id;
        var alias = await _dbContext.MasterTestingTypeAliases.AsNoTracking().FirstOrDefaultAsync(item => item.Alias == value.Trim(), cancellationToken);
        return alias?.TestingTypeId;
    }

    private async Task<Client> ResolveClientAsync(string value, CancellationToken cancellationToken)
    {
        var trimmed = value.Trim();
        var code = NormalizeClientCode(trimmed);
        var client = await _dbContext.Clients.FirstOrDefaultAsync(item => item.Code == code, cancellationToken);
        if (client != null) return client;
        var alias = await _dbContext.ClientAliases.AsNoTracking().FirstOrDefaultAsync(item => item.Alias == trimmed, cancellationToken);
        if (alias != null) return await _dbContext.Clients.FirstAsync(item => item.Id == alias.ClientId, cancellationToken);

        client = new Client { Code = code, Name = trimmed };
        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return client;
    }

    private static bool TryParsePrecondition(string value, out ParsedPrecondition parsed)
    {
        parsed = new ParsedPrecondition(string.Empty, [], false, false);
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || trimmed.Equals("All user", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("All roles", StringComparison.OrdinalIgnoreCase)) return true;
        if (IsGlobalParameter(trimmed)) return true;

        var globalParameter = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(.*?)\s+Global\s+parameter\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (globalParameter.Success && TryExtractRole(globalParameter.Groups[1].Value, out var globalRole, out var globalTrailing, out var globalShared) && string.IsNullOrWhiteSpace(globalTrailing))
        {
            parsed = new ParsedPrecondition(globalRole, [], globalShared, false);
            return true;
        }

        var paren = System.Text.RegularExpressions.Regex.Match(trimmed, @"^(.*?)\s*\((.+)\)\s*$");
        if (paren.Success && TryExtractRole(paren.Groups[1].Value, out var parenRole, out _, out var parenShared))
        {
            parsed = new ParsedPrecondition(parenRole, SplitClientTokens(paren.Groups[2].Value), parenShared, false);
            return true;
        }

        if (trimmed.Contains('_'))
        {
            var parts = trimmed.Split('_', 2);
            if (TryExtractRole(parts[0], out var underscoreRole, out _, out var underscoreShared))
            {
                parsed = new ParsedPrecondition(underscoreRole, SplitClientTokens(parts[1]), underscoreShared, false);
                return true;
            }
        }

        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\s+books\s*$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            && TryExtractRole(System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s+books\s*$", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase), out var bookRole, out var remainder, out var bookShared))
        {
            parsed = new ParsedPrecondition(bookRole, SplitClientTokens(remainder), bookShared, true);
            return true;
        }

        if (TryExtractRole(trimmed, out var role, out var trailing, out var shared) && string.IsNullOrWhiteSpace(trailing))
        {
            parsed = new ParsedPrecondition(role, [], shared, false);
            return true;
        }

        return false;
    }

    private static bool TryExtractRole(string value, out string role, out string remainder, out bool isShared)
    {
        var normalized = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"(\s+Role)+", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        foreach (var candidate in RolePrefixes.OrderByDescending(item => item.Raw.Length))
        {
            if (normalized.Equals(candidate.Raw, StringComparison.OrdinalIgnoreCase))
            {
                role = candidate.Role;
                remainder = string.Empty;
                isShared = candidate.Shared;
                return true;
            }

            if (normalized.StartsWith(candidate.Raw + " ", StringComparison.OrdinalIgnoreCase))
            {
                role = candidate.Role;
                remainder = normalized[(candidate.Raw.Length + 1)..].Trim();
                isShared = candidate.Shared;
                return true;
            }
        }

        role = string.Empty;
        remainder = value;
        isShared = false;
        return false;
    }

    private static IReadOnlyList<string> SplitClientTokens(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Equals("T & F", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("T&F", StringComparison.OrdinalIgnoreCase)) return [trimmed];
        return trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IReadOnlyList<string> SplitTestingTypes(string value) => value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    private static string NormalizeTestingType(string value) => value.Trim().Equals("Tomcat_Regression", StringComparison.OrdinalIgnoreCase) ? "Tomcat_Reg" : value.Trim();
    private static string NormalizeClientCode(string value) => value.Trim().ToUpperInvariant().Replace(" ", string.Empty).Replace("&", "N");
    private static bool IsGlobalParameter(string value) => value.Trim().Equals("Global parameter", StringComparison.OrdinalIgnoreCase);
    private static string NormalizeText(string value) => value.Replace("\r\n", "\n").Replace('\r', '\n');
    private static string NormalizeCell(string? value) => NormalizeText(value ?? string.Empty).Trim();
    private static readonly string[] KnownTestingTypes = ["Basic", "Mock", "Browser", "Regression", "Tomcat_Reg"];
    private static readonly string[] KnownRoles = ["Author", "PE", "Collator", "Editor"];
    private static readonly (string Raw, string Role, bool Shared)[] RolePrefixes =
    [
        ("Shared Author", "Author", true),
        ("Shared Editor", "Editor", true),
        ("Shared Collator", "Collator", true),
        ("Co Author", "Author", true),
        ("Author", "Author", false),
        ("Collator", "Collator", false),
        ("Editor", "Editor", false),
        ("PE", "PE", false)
    ];

    private static string TempUploadDirectory() => Path.Combine(Path.GetTempPath(), "impact-testcaseviewer-imports");
    private static string MetadataPath(string token) => Path.Combine(TempUploadDirectory(), $"{token}.json");

    private static async Task<TempUploadMetadata?> ReadTempUploadAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        var metadataPath = MetadataPath(token);
        if (!File.Exists(metadataPath)) return null;
        var metadata = JsonSerializer.Deserialize<TempUploadMetadata>(await File.ReadAllTextAsync(metadataPath, cancellationToken));
        if (metadata == null || metadata.ExpiresAt < DateTimeOffset.UtcNow || !File.Exists(metadata.StoredPath))
        {
            if (metadata != null) DeleteTempUpload(metadata);
            return null;
        }
        return metadata;
    }

    private static void DeleteTempUpload(TempUploadMetadata metadata)
    {
        TryDelete(metadata.StoredPath);
        TryDelete(MetadataPath(metadata.Token));
    }

    private static void CleanupExpiredUploads()
    {
        var directory = TempUploadDirectory();
        if (!Directory.Exists(directory)) return;
        foreach (var metadataPath in Directory.EnumerateFiles(directory, "*.json"))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<TempUploadMetadata>(File.ReadAllText(metadataPath));
                if (metadata == null || metadata.ExpiresAt < DateTimeOffset.UtcNow) DeleteTempUpload(metadata ?? new TempUploadMetadata { Token = Path.GetFileNameWithoutExtension(metadataPath) });
            }
            catch
            {
                TryDelete(metadataPath);
            }
        }
    }

    private static void TryDelete(string path)
    {
        try { if (!string.IsNullOrWhiteSpace(path) && File.Exists(path)) File.Delete(path); } catch { }
    }

    private sealed record ParsedUpload(IReadOnlyList<ParsedRow> Rows, IReadOnlyList<QaImportBatchError> Errors);
    private sealed record ParsedRow(QaRow Row, int SourceRowNumber);
    private sealed record ParsedSheet(string SourceName, List<List<string>> Records);
    private sealed record ParsedPrecondition(string Role, IReadOnlyList<string> ClientTokens, bool IsSharedRole, bool IsBook);
    private sealed record ResolvedPrecondition(int? RoleId, IReadOnlyList<int> ClientIds, IReadOnlyList<string> ClientCodes, bool IsSharedRole, bool IsBook, int? MasterTypeId);
    private sealed class TempUploadMetadata
    {
        public string Token { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StoredPath { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
    }
}

file static class ImportSheetExtensions
{
    public static int SourceRowStart(this QaImportBatchSheet sheet) => sheet.Rows.OrderBy(row => row.SourceRowNumber).FirstOrDefault()?.SourceRowNumber ?? 0;
}
