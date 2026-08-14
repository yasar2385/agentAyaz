using ImpactSupport.Api.TestCaseViewer.Services;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class QaTsvRowReaderTests
{
    [Fact]
    public void ReadRows_ParsesOfflineRowsFromTsv()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"qa-rows-{Guid.NewGuid():N}.tsv");
        System.IO.File.WriteAllLines(path,
        [
            "SheetName\tTestCaseNo\tTestCaseId\tModule\tDescription\tQAStatus\tDevStatus\tIssueType\tActualResult\tQARemarks\tDevRemarks\tRounds",
            "Login\t1\tTC-1\tAuth\tCan login\tPass\tFixed\tBug\tOk\tQA note\tDev note\tR1: QA=Pass; Dev=Fixed"
        ]);

        try
        {
            var rows = new QaTsvRowReader().ReadRows(path);

            var row = Assert.Single(rows);
            Assert.Equal("Login", row.SheetName);
            Assert.Equal("TC-1", row.TestCaseId);
            Assert.Equal("Auth", row.Module);
            Assert.Equal("Pass", row.QaStatus);
            Assert.Equal("Fixed", Assert.Single(row.Rounds).DevStatus);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void ReadRows_ReturnsEmpty_WhenFileIsMissing()
    {
        var rows = new QaTsvRowReader().ReadRows(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "missing.tsv"));

        Assert.Empty(rows);
    }
}
