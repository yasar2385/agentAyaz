using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleSheetsService : IGoogleSheetsService
{
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

    public async Task<DashboardSummary> GetDashboardSummaryAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var sheets = await GetSheetsAsync(fileId, cancellationToken);
        var summary = new DashboardSummary
        {
            FileId = fileId,
            TotalSheets = sheets.Count
        };

        foreach (var sheet in sheets)
        {
            var rows = await ReadParsedRowsAsync(fileId, sheet.Name, cancellationToken);
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
        var range = $"'{EscapeSheetName(sheetName)}'!A:Z";
        var request = _sheetsService.Spreadsheets.Values.Get(fileId, range);
        var response = await request.ExecuteAsync(cancellationToken);
        return _parser.ParseRows(fileId, sheetName, response.Values ?? []);
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
}
