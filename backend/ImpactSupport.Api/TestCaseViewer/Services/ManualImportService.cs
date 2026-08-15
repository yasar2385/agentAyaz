using System.Text;
using System.Text.Json;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using Microsoft.EntityFrameworkCore;

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

        BuildBatch(batch, parsed.Rows, parsed.Errors, existingSheetNames);
        _dbContext.QaImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch);
    }

    public async Task<ImportBatchResponse> UploadResultsAsync(IReadOnlyList<IFormFile> files, string resultMode, AuthUser? user, CancellationToken cancellationToken = default)
    {
        if (files.Count == 0) throw new ArgumentException("At least one result file is required.", nameof(files));

        var normalizedMode = resultMode.Equals("regression", StringComparison.OrdinalIgnoreCase) ? "regression" : "single";
        var parsed = await ParseFilesAsync(files, ResultKind, cancellationToken);
        var batch = new QaImportBatch
        {
            UploadKind = ResultKind,
            ResultMode = normalizedMode,
            FileName = string.Join(", ", files.Select(file => file.FileName)),
            UploadedBy = user?.Username ?? string.Empty,
            Status = DryRunStatus,
            RowsError = parsed.Errors.Count
        };

        BuildBatch(batch, parsed.Rows, parsed.Errors, []);
        _dbContext.QaImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(batch);
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
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            var records = ParseDelimited(text, DetectDelimiter(file.FileName, text));
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

    private static void BuildBatch(QaImportBatch batch, IReadOnlyList<ParsedRow> parsedRows, IReadOnlyList<QaImportBatchError> errors, HashSet<string> existingSheetNames)
    {
        foreach (var error in errors) batch.Errors.Add(error);

        var duplicateIds = parsedRows
            .GroupBy(item => item.Row.TestCaseId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var duplicate in duplicateIds)
        {
            batch.Errors.Add(new QaImportBatchError
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
        batch.RowsError = batch.Errors.Count;
    }

    private static QaRow ToQaRow(string fileName, IReadOnlyList<string> record, IReadOnlyDictionary<string, List<int>> map, string uploadKind)
    {
        var sheetName = Get(record, map, "Sheet Name");
        if (string.IsNullOrWhiteSpace(sheetName)) sheetName = Get(record, map, "SheetName");
        if (string.IsNullOrWhiteSpace(sheetName)) sheetName = Path.GetFileNameWithoutExtension(fileName);

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
            QaRemarks = GetRepeated(record, map, "QA Remarks", "QARemarks"),
            DevRemarks = GetRepeated(record, map, "Dev. Remarks", "DevRemarks"),
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

    private static ImportBatchResponse ToResponse(QaImportBatch batch)
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

    private static List<List<string>> ParseDelimited(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == delimiter && !inQuotes)
            {
                row.Add(cell.ToString().Trim());
                cell.Clear();
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                row.Add(cell.ToString().Trim());
                cell.Clear();
                rows.Add(row);
                row = [];
            }
            else
            {
                cell.Append(ch);
            }
        }

        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString().Trim());
            rows.Add(row);
        }

        return rows;
    }

    private static char DetectDelimiter(string fileName, string text)
    {
        if (fileName.EndsWith(".tsv", StringComparison.OrdinalIgnoreCase)) return '\t';
        if (fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return ',';
        var firstLine = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
        return firstLine.Count(ch => ch == '\t') > firstLine.Count(ch => ch == ',') ? '\t' : ',';
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

    private static List<string> GetRepeated(IReadOnlyList<string> row, IReadOnlyDictionary<string, List<int>> map, params string[] headers)
    {
        return headers
            .Where(map.ContainsKey)
            .SelectMany(header => map[header])
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

    private sealed record ParsedUpload(IReadOnlyList<ParsedRow> Rows, IReadOnlyList<QaImportBatchError> Errors);
    private sealed record ParsedRow(QaRow Row, int SourceRowNumber);
}
