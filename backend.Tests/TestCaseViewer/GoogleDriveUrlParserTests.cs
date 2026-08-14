using ImpactSupport.Api.TestCaseViewer.Services;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class GoogleDriveUrlParserTests
{
    [Fact]
    public void Parse_SpreadsheetUrl_ExtractsFileIdAndGid()
    {
        var parser = new GoogleDriveUrlParser();

        var result = parser.Parse("https://docs.google.com/spreadsheets/d/sheet-123/edit#gid=456");

        Assert.Equal(GoogleDriveUrlKind.Spreadsheet, result.Kind);
        Assert.Equal("sheet-123", result.Id);
        Assert.Equal(456, result.SheetGid);
    }

    [Fact]
    public void Parse_FolderUrl_ExtractsFolderId()
    {
        var parser = new GoogleDriveUrlParser();

        var result = parser.Parse("https://drive.google.com/drive/folders/folder-123?usp=sharing");

        Assert.Equal(GoogleDriveUrlKind.Folder, result.Kind);
        Assert.Equal("folder-123", result.Id);
    }

    [Fact]
    public void Parse_BareId_TreatsValueAsSpreadsheetSource()
    {
        var parser = new GoogleDriveUrlParser();

        var result = parser.Parse("sheet-or-folder-id");

        Assert.Equal(GoogleDriveUrlKind.Spreadsheet, result.Kind);
        Assert.Equal("sheet-or-folder-id", result.Id);
        Assert.Equal("https://docs.google.com/spreadsheets/d/sheet-or-folder-id/edit", result.NormalizedUrl);
    }
}
