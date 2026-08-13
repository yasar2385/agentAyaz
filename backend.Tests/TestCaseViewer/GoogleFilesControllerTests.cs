using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ImpactSupport.Api.TestCaseViewer.Controllers;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public class GoogleFilesControllerTests
{
    private static GoogleFilesController CreateController(IGoogleDriveService driveService)
    {
        var sheetsService = new Mock<IGoogleSheetsService>();
        return new GoogleFilesController(
            driveService,
            sheetsService.Object,
            Options.Create(new TestCaseViewerOptions()));
    }

    [Fact]
    public async Task GetFiles_ReturnsOk_WithFiles()
    {
        var mockService = new Mock<IGoogleDriveService>();
        mockService.Setup(s => s.GetFilesAsync("master", It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<GoogleFileInfo>
            {
                new GoogleFileInfo { Id = "1", Name = "A", MimeType = "application/vnd.google-apps.spreadsheet" }
            } as IReadOnlyList<GoogleFileInfo>
        );

        var controller = CreateController(mockService.Object);

        var result = await controller.GetFiles("master");

        var ok = Assert.IsType<OkObjectResult>(result);
        var files = Assert.IsAssignableFrom<IEnumerable<GoogleFileInfo>>(ok.Value);
        Assert.Single(files);
    }

    [Fact]
    public async Task GetFile_ReturnsNotFound_WhenMissing()
    {
        var mockService = new Mock<IGoogleDriveService>();
        mockService.Setup(s => s.GetFileAsync("1", It.IsAny<CancellationToken>())).ReturnsAsync((GoogleFileInfo?)null);

        var controller = CreateController(mockService.Object);

        var result = await controller.GetFile("1");

        Assert.IsType<NotFoundResult>(result);
    }
}
