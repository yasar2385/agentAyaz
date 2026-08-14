using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class GoogleDriveServiceTests
{
    private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    [Fact]
    public async Task GetFilesAsync_MasterListsOnlyDirectFolderSpreadsheets()
    {
        var fileLister = new Mock<IGoogleDriveFileLister>();
        fileLister
            .Setup(l => l.ListFilesAsync(
                "master-root",
                SpreadsheetMimeType,
                "modifiedTime desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<File>
            {
                CreateFile("master-1", "Master spreadsheet", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-10T10:00:00Z"))
            });

        var service = CreateService(fileLister.Object);

        var files = await service.GetFilesAsync("master");

        var file = Assert.Single(files);
        Assert.Equal("master-1", file.Id);
        fileLister.Verify(
            l => l.ListFilesAsync("master-root", FolderMimeType, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetFilesAsync_RegressionRecursesNestedFoldersFiltersPrefixAndSorts()
    {
        var fileLister = new Mock<IGoogleDriveFileLister>();

        SetupList(fileLister, "regression-root", SpreadsheetMimeType, new[]
        {
            CreateFile("direct-old", "Regression Direct Old", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-01T10:00:00Z")),
            CreateFile("direct-skip", "Smoke Direct", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-14T10:00:00Z"))
        });
        SetupList(fileLister, "regression-root", FolderMimeType, new[]
        {
            CreateFile("folder-a", "Folder A", FolderMimeType, null)
        });

        SetupList(fileLister, "folder-a", SpreadsheetMimeType, new[]
        {
            CreateFile("nested-new", "Regression Nested New", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-12T10:00:00Z"))
        });
        SetupList(fileLister, "folder-a", FolderMimeType, new[]
        {
            CreateFile("folder-b", "Folder B", FolderMimeType, null)
        });

        SetupList(fileLister, "folder-b", SpreadsheetMimeType, new[]
        {
            CreateFile("deep-middle", "Regression Deep Middle", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-10T10:00:00Z"))
        });
        SetupList(fileLister, "folder-b", FolderMimeType, Array.Empty<DriveFile>());

        var service = CreateService(fileLister.Object);

        var files = await service.GetFilesAsync("regression");

        Assert.Collection(
            files,
            file => Assert.Equal("nested-new", file.Id),
            file => Assert.Equal("deep-middle", file.Id),
            file => Assert.Equal("direct-old", file.Id));
    }

    [Fact]
    public async Task GetFilesAsync_RegressionUsesAllNestedLevels()
    {
        var fileLister = new Mock<IGoogleDriveFileLister>();

        SetupList(fileLister, "regression-root", SpreadsheetMimeType, Array.Empty<DriveFile>());
        SetupList(fileLister, "regression-root", FolderMimeType, new[]
        {
            CreateFile("level-1", "Level 1", FolderMimeType, null)
        });

        SetupList(fileLister, "level-1", SpreadsheetMimeType, Array.Empty<DriveFile>());
        SetupList(fileLister, "level-1", FolderMimeType, new[]
        {
            CreateFile("level-2", "Level 2", FolderMimeType, null)
        });

        SetupList(fileLister, "level-2", SpreadsheetMimeType, new[]
        {
            CreateFile("deep-file", "Regression Deep File", SpreadsheetMimeType, DateTimeOffset.Parse("2026-08-14T10:00:00Z"))
        });
        SetupList(fileLister, "level-2", FolderMimeType, Array.Empty<DriveFile>());

        var service = CreateService(fileLister.Object);

        var files = await service.GetFilesAsync("regression");

        var file = Assert.Single(files);
        Assert.Equal("deep-file", file.Id);
    }

    private static GoogleDriveService CreateService(IGoogleDriveFileLister fileLister)
    {
        return new GoogleDriveService(
            fileLister,
            Options.Create(new GoogleOptions
            {
                MasterFolderId = "master-root",
                RegressionFolderId = "regression-root",
                RegressionFilePrefix = "Regression"
            }));
    }

    private static void SetupList(
        Mock<IGoogleDriveFileLister> fileLister,
        string folderId,
        string mimeType,
        IReadOnlyList<DriveFile> files)
    {
        fileLister
            .Setup(l => l.ListFilesAsync(
                folderId,
                mimeType,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);
    }

    private static DriveFile CreateFile(
        string id,
        string name,
        string mimeType,
        DateTimeOffset? modifiedTime)
    {
        return new DriveFile
        {
            Id = id,
            Name = name,
            MimeType = mimeType,
            ModifiedTimeDateTimeOffset = modifiedTime
        };
    }
}
