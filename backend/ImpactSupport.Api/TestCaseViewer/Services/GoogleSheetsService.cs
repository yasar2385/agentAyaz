using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google;
using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleSheetsService : IGoogleSheetsService
{
    private const int QaHeaderSpreadsheetRow = 22;
    private readonly SheetsService _sheetsService;
    private readonly IQaSheetParser _parser;

    public GoogleSheetsService(IGoogleCredentialProvider credentialProvider, IQaSheetParser parser)
    {
        _parser = parser;
        _sheetsService = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credentialProvider.GetCredential(),
            ApplicationName = "ImpactSupport.TestCaseViewer"
        });
    }

    public async Task<IReadOnlyList<SheetInfo>> GetSheetsAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var request = _sheetsService.Spreadsheets.Get(fileId);
        request.Fields = "sheets(properties(title,index,gridProperties(rowCount,columnCount)))";
        var spreadsheet = await request.ExecuteAsync(cancellationToken);

        return spreadsheet.Sheets
            .Select(sheet => new SheetInfo
                {
                    Name = sheet.Properties.Title,
                    Index = sheet.Properties.Index,
                    SheetId = sheet.Properties.SheetId,
                    RowCount = sheet.Properties.GridProperties?.RowCount ?? 0,
                    ColumnCount = sheet.Properties.GridProperties?.ColumnCount ?? 0
                })
            .OrderBy(sheet => sheet.Index)
            .ToList();
    }

    public async Task<SheetRowsResponse> GetRowsAsync(string fileId, string sheetName, CancellationToken cancellationToken = default)
    {
        var rows = await ReadParsedRowsAsync(fileId, sheetName, cancellationToken);

        return new SheetRowsResponse
        {
            FileId = fileId,
            SheetName = sheetName,
            Rows = rows,
            QaStatuses = DistinctStatuses(rows.Select(row => row.QaStatus)),
            DevStatuses = DistinctStatuses(rows.Select(row => row.DevStatus))
        };
    }

    public async Task<IList<IList<object>>> GetValuesAsync(
        string fileId,
        string sheetName,
        CancellationToken cancellationToken = default)
    {
        return await GetValuesForRangeAsync(fileId, sheetName, BuildQaRowsRange(sheetName), cancellationToken);
    }

    public async Task<IList<IList<object>>> GetAllValuesAsync(
        string fileId,
        string sheetName,
        CancellationToken cancellationToken = default)
    {
        return await GetValuesForRangeAsync(fileId, sheetName, BuildSheetMetadataRange(sheetName), cancellationToken);
    }

    public async Task UpdateFieldsAsync(
        string fileId,
        string sheetName,
        IReadOnlyList<QaFieldEdit> edits,
        CancellationToken cancellationToken = default)
    {
        if (edits.Count == 0)
        {
            return;
        }

        var values = await GetValuesAsync(fileId, sheetName, cancellationToken);
        if (values.Count == 0)
        {
            return;
        }

        var headers = values[0].Select(CellText).ToList();
        var map = BuildHeaderMap(headers);
        var updates = new List<ValueRange>();

        foreach (var edit in edits)
        {
            if (!map.TryGetValue(edit.FieldName, out var columnIndex))
            {
                continue;
            }

            var rowIndex = FindRowIndex(values, map, edit);
            if (rowIndex < 0)
            {
                continue;
            }

            updates.Add(new ValueRange
            {
                Range = $"'{EscapeSheetName(sheetName)}'!{ColumnName(columnIndex + 1)}{QaHeaderSpreadsheetRow + rowIndex}",
                Values = [[edit.Value]]
            });
        }

        if (updates.Count == 0)
        {
            return;
        }

        var request = _sheetsService.Spreadsheets.Values.BatchUpdate(
            new BatchUpdateValuesRequest
            {
                ValueInputOption = "USER_ENTERED",
                Data = updates
            },
            fileId);
        await request.ExecuteAsync(cancellationToken);
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var sheets = await GetSheetsAsync(fileId, cancellationToken);
        var summary = new DashboardSummary
        {
            FileId = fileId,
            TotalSheets = sheets.Count
        };

        if (sheets.Count == 0)
        {
            return summary;
        }

        var request = _sheetsService.Spreadsheets.Values.BatchGet(fileId);
        request.Ranges = sheets.Select(sheet => BuildQaRowsRange(sheet.Name)).ToList();
        var response = await request.ExecuteAsync(cancellationToken);
        var valueRanges = response.ValueRanges ?? [];

        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            var values = i < valueRanges.Count ? valueRanges[i].Values ?? [] : [];
            var rows = _parser.ParseRows(fileId, sheet.Name, values);
            var sheetSummary = new SheetSummary
            {
                SheetName = sheet.Name,
                Module = rows.Select(row => row.Module).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty,
                TotalTestCases = rows.Count,
                QaStatusCounts = CountStatuses(rows.Select(row => row.QaStatus)),
                DevStatusCounts = CountStatuses(rows.Select(row => row.DevStatus))
            };

            summary.Sheets.Add(sheetSummary);
            summary.TotalTestCases += sheetSummary.TotalTestCases;
            MergeCounts(summary.QaStatusCounts, sheetSummary.QaStatusCounts);
            MergeCounts(summary.DevStatusCounts, sheetSummary.DevStatusCounts);
        }

        return summary;
    }

    private async Task<IReadOnlyList<QaRow>> ReadParsedRowsAsync(string fileId, string sheetName, CancellationToken cancellationToken)
    {
        return _parser.ParseRows(fileId, sheetName, await GetValuesAsync(fileId, sheetName, cancellationToken));
    }

    private async Task<IList<IList<object>>> GetValuesForRangeAsync(
        string fileId,
        string sheetName,
        string range,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = _sheetsService.Spreadsheets.Values.Get(fileId, range);
            var response = await request.ExecuteAsync(cancellationToken);
            return response.Values ?? [];
        }
        catch (GoogleApiException ex) when (ex.Message.Contains("Unable to parse range", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unable to parse range '{range}' for sheet '{sheetName}'.", ex);
        }
    }

    private static IReadOnlyList<string> DistinctStatuses(IEnumerable<string> statuses)
    {
        return statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(status => status)
            .ToList();
    }

    private static Dictionary<string, int> CountStatuses(IEnumerable<string> statuses)
    {
        return statuses
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .GroupBy(status => status, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static void MergeCounts(Dictionary<string, int> target, Dictionary<string, int> source)
    {
        foreach (var (key, value) in source)
        {
            target[key] = target.GetValueOrDefault(key) + value;
        }
    }

    private static string EscapeSheetName(string sheetName) => sheetName.Replace("'", "''");

    public static string BuildQaRowsRange(string sheetName) => $"'{EscapeSheetName(sheetName)}'!A22:Z";

    public static string BuildSheetMetadataRange(string sheetName) => $"'{EscapeSheetName(sheetName)}'!A:Z";

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(header[i]) && !map.ContainsKey(header[i]))
            {
                map[header[i]] = i;
            }
        }

        return map;
    }

    private static int FindRowIndex(IList<IList<object>> values, IReadOnlyDictionary<string, int> map, QaFieldEdit edit)
    {
        for (var i = 1; i < values.Count; i++)
        {
            var row = values[i];
            var testCaseId = Get(row, map, "Test Case ID");
            var testCaseNo = Get(row, map, "Test Case No.");
            if ((!string.IsNullOrWhiteSpace(edit.TestCaseId) && string.Equals(testCaseId, edit.TestCaseId, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(edit.TestCaseNo) && string.Equals(testCaseNo, edit.TestCaseNo, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Get(IList<object> row, IReadOnlyDictionary<string, int> map, string header)
    {
        return map.TryGetValue(header, out var index) && index < row.Count ? CellText(row[index]) : string.Empty;
    }

    private static string ColumnName(int columnNumber)
    {
        var dividend = columnNumber;
        var columnName = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar('A' + modulo) + columnName;
            dividend = (dividend - modulo) / 26;
        }

        return columnName;
    }

    private static string CellText(object? value) => value?.ToString()?.Trim() ?? string.Empty;
}
