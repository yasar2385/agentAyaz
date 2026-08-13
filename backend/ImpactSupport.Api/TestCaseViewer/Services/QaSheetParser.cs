using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class QaSheetParser : IQaSheetParser
{
    private const int HeaderRowIndex = 22;

    public IReadOnlyList<QaRow> ParseRows(string fileId, string sheetName, IList<IList<object>> values)
    {
        if (values.Count <= HeaderRowIndex)
        {
            return [];
        }

        var header = values[HeaderRowIndex].Select(CellText).ToList();
        var map = BuildHeaderMap(header);
        var rows = new List<QaRow>();

        for (var i = HeaderRowIndex + 1; i < values.Count; i++)
        {
            var raw = values[i];
            var row = new QaRow
            {
                SourceFileId = fileId,
                SheetName = sheetName,
                TestCaseNo = Get(raw, map, "Test Case No."),
                TestCaseId = Get(raw, map, "Test Case ID"),
                Preconditions = Get(raw, map, "Preconditions"),
                Module = Get(raw, map, "Module/ Sub Module"),
                PreparedBy = Get(raw, map, "Testcase Prepared Person Name"),
                PreparedDate = Get(raw, map, "Testcase Prepared Date"),
                TestingType = Get(raw, map, "Type of testing"),
                Description = Get(raw, map, "Test Case Description"),
                TestCases = Get(raw, map, "Test Cases"),
                TestData = Get(raw, map, "Test Data"),
                ExpectedResult = Get(raw, map, "Expected Result"),
                ActualResult = Get(raw, map, "Actual Result"),
                IssueType = Get(raw, map, "Issue Type"),
                QaStatus = Get(raw, map, "QA Status"),
                DevStatus = Get(raw, map, "Dev. Status"),
                QaRemarks = GetRepeated(raw, header, "QA Remarks"),
                DevRemarks = GetRepeated(raw, header, "Dev. Remarks")
            };

            if (HasContent(row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

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

    private static string Get(IList<object> row, IReadOnlyDictionary<string, int> map, string header)
    {
        return map.TryGetValue(header, out var index) && index < row.Count ? CellText(row[index]) : string.Empty;
    }

    private static List<string> GetRepeated(IList<object> row, IReadOnlyList<string> header, string name)
    {
        return header
            .Select((value, index) => new { value, index })
            .Where(x => string.Equals(x.value, name, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.index < row.Count ? CellText(row[x.index]) : string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }

    private static bool HasContent(QaRow row)
    {
        return !string.IsNullOrWhiteSpace(row.TestCaseNo)
            || !string.IsNullOrWhiteSpace(row.TestCaseId)
            || !string.IsNullOrWhiteSpace(row.Module)
            || !string.IsNullOrWhiteSpace(row.QaStatus)
            || !string.IsNullOrWhiteSpace(row.DevStatus);
    }

    private static string CellText(object? value) => value?.ToString()?.Trim() ?? string.Empty;
}
