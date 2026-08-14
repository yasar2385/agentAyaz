using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IQaTsvRowReader
{
    IReadOnlyList<QaRow> ReadRows(string path);
}

public sealed class QaTsvRowReader : IQaTsvRowReader
{
    public IReadOnlyList<QaRow> ReadRows(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return [];
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length <= 1)
        {
            return [];
        }

        var headers = Split(lines[0]);
        var map = headers
            .Select((header, index) => new { header, index })
            .Where(item => !string.IsNullOrWhiteSpace(item.header))
            .ToDictionary(item => item.header, item => item.index, StringComparer.OrdinalIgnoreCase);

        return lines
            .Skip(1)
            .Select(line => ToRow(Split(line), map))
            .Where(HasContent)
            .ToList();
    }

    private static QaRow ToRow(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> map)
    {
        return new QaRow
        {
            SheetName = Get(cells, map, "SheetName"),
            TestCaseNo = Get(cells, map, "TestCaseNo"),
            TestCaseId = Get(cells, map, "TestCaseId"),
            Module = Get(cells, map, "Module"),
            Description = Get(cells, map, "Description"),
            QaStatus = Get(cells, map, "QAStatus"),
            DevStatus = Get(cells, map, "DevStatus"),
            IssueType = Get(cells, map, "IssueType"),
            ActualResult = Get(cells, map, "ActualResult"),
            QaRemarks = SplitList(Get(cells, map, "QARemarks")),
            DevRemarks = SplitList(Get(cells, map, "DevRemarks")),
            Rounds = ParseRounds(Get(cells, map, "Rounds"))
        };
    }

    private static List<QaRound> ParseRounds(string value)
    {
        var rounds = new List<QaRound>();
        foreach (var part in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var round = new QaRound();
            var pieces = part.Split(':', 2, StringSplitOptions.TrimEntries);
            if (pieces.Length > 0 && pieces[0].StartsWith("R", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(pieces[0][1..], out var number))
            {
                round.RoundNumber = number;
            }

            if (pieces.Length == 2)
            {
                foreach (var status in pieces[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var statusParts = status.Split('=', 2, StringSplitOptions.TrimEntries);
                    if (statusParts.Length != 2) continue;
                    if (statusParts[0].Equals("QA", StringComparison.OrdinalIgnoreCase)) round.QaStatus = statusParts[1];
                    if (statusParts[0].Equals("Dev", StringComparison.OrdinalIgnoreCase)) round.DevStatus = statusParts[1];
                }
            }

            if (round.RoundNumber > 0 || !string.IsNullOrWhiteSpace(round.QaStatus) || !string.IsNullOrWhiteSpace(round.DevStatus))
            {
                rounds.Add(round);
            }
        }

        return rounds;
    }

    private static List<string> SplitList(string value)
    {
        return value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string Get(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> map, string header)
    {
        return map.TryGetValue(header, out var index) && index < cells.Count ? cells[index].Trim() : string.Empty;
    }

    private static IReadOnlyList<string> Split(string line) => line.Split('\t');

    private static bool HasContent(QaRow row)
    {
        return !string.IsNullOrWhiteSpace(row.TestCaseNo)
            || !string.IsNullOrWhiteSpace(row.TestCaseId)
            || !string.IsNullOrWhiteSpace(row.Module)
            || !string.IsNullOrWhiteSpace(row.QaStatus)
            || !string.IsNullOrWhiteSpace(row.DevStatus);
    }
}
