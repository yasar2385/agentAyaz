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

    [Fact]
    public void ParseRows_TreatsFirstReturnedRowAsHeader()
    {
        var parser = new QaSheetParser();
        var values = CreateValues(
            ["Test Case No.", "Test Case ID", "Module/ Sub Module", "QA Status"],
            ["1", "TC-1", "Billing", "Pass"]);

        var row = Assert.Single(parser.ParseRows("file-1", "Sheet A", values));

        Assert.Equal("1", row.TestCaseNo);
        Assert.Equal("TC-1", row.TestCaseId);
        Assert.Equal("Billing", row.Module);
        Assert.Equal("Pass", row.QaStatus);
    }

    [Fact]
    public void BuildQaRowsRange_StartsAtA22AndEscapesSheetNames()
    {
        var range = GoogleSheetsService.BuildQaRowsRange("Regression testing _CIRCCQO-2025-012757_QA_LIVE_10.08.26_After Live_DK");
        var escapedRange = GoogleSheetsService.BuildQaRowsRange("Owner's QA");

        Assert.Equal("'Regression testing _CIRCCQO-2025-012757_QA_LIVE_10.08.26_After Live_DK'!A22:Z", range);
        Assert.Equal("'Owner''s QA'!A22:Z", escapedRange);
    }

    private static IList<IList<object>> CreateValues(IList<object> header, IList<object> row)
    {
        return [header, row];
    }
}
