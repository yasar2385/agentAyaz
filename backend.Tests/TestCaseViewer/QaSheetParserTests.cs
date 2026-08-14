using ImpactSupport.Api.TestCaseViewer.Services;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class QaSheetParserTests
{
    [Fact]
    public void ParseRows_CreatesRoundsFromRepeatedQaAndDevStatusPairs()
    {
        var parser = new QaSheetParser();
        var values = CreateValues(
            ["Test Case No.", "Module/ Sub Module", "QA Status", "Dev. Status", "QA Status", "Dev. Status"],
            ["1", "Billing", "Fail", "Fixed", "Pass", "Closed"]);

        var rows = parser.ParseRows("file-1", "Sheet A", values);

        var row = Assert.Single(rows);
        Assert.Equal("Fail", row.QaStatus);
        Assert.Equal("Fixed", row.DevStatus);
        Assert.Collection(
            row.Rounds,
            round =>
            {
                Assert.Equal(1, round.RoundNumber);
                Assert.Equal("Fail", round.QaStatus);
                Assert.Equal("Fixed", round.DevStatus);
            },
            round =>
            {
                Assert.Equal(2, round.RoundNumber);
                Assert.Equal("Pass", round.QaStatus);
                Assert.Equal("Closed", round.DevStatus);
            });
    }

    [Fact]
    public void ParseRows_SkipsBlankRepeatedStatusPairs()
    {
        var parser = new QaSheetParser();
        var values = CreateValues(
            ["Test Case No.", "Module/ Sub Module", "QA Status", "Dev. Status", "QA Status", "Dev. Status", "QA Status", "Dev. Status"],
            ["1", "Billing", "Fail", "Fixed", "", "", "Pass", "Closed"]);

        var rows = parser.ParseRows("file-1", "Sheet A", values);

        var row = Assert.Single(rows);
        Assert.Collection(
            row.Rounds,
            round => Assert.Equal(1, round.RoundNumber),
            round => Assert.Equal(3, round.RoundNumber));
    }

    [Fact]
    public void ParseRows_CreatesRoundWhenOnlyOneStatusInPairHasValue()
    {
        var parser = new QaSheetParser();
        var values = CreateValues(
            ["Test Case No.", "Module/ Sub Module", "QA Status", "Dev. Status", "QA Status", "Dev. Status"],
            ["1", "Billing", "", "WIP", "Pass", ""]);

        var rows = parser.ParseRows("file-1", "Sheet A", values);

        var row = Assert.Single(rows);
        Assert.Collection(
            row.Rounds,
            round =>
            {
                Assert.Equal(1, round.RoundNumber);
                Assert.Equal(string.Empty, round.QaStatus);
                Assert.Equal("WIP", round.DevStatus);
            },
            round =>
            {
                Assert.Equal(2, round.RoundNumber);
                Assert.Equal("Pass", round.QaStatus);
                Assert.Equal(string.Empty, round.DevStatus);
            });
    }

    private static IList<IList<object>> CreateValues(IList<object> header, IList<object> row)
    {
        var values = new List<IList<object>>();
        for (var i = 0; i < 22; i++)
        {
            values.Add([]);
        }

        values.Add(header);
        values.Add(row);
        return values;
    }
}
