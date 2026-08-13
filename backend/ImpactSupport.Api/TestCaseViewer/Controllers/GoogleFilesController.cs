using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/files")]
public sealed class GoogleFilesController : ControllerBase
{
    private readonly IGoogleDriveService _driveService;
    private readonly IGoogleSheetsService _sheetsService;
    private readonly TestCaseViewerOptions _options;

    public GoogleFilesController(
        IGoogleDriveService driveService,
        IGoogleSheetsService sheetsService,
        IOptions<TestCaseViewerOptions> options)
    {
        _driveService = driveService;
        _sheetsService = sheetsService;
        _options = options.Value;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles([FromQuery] string reportType = "master", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reportType))
            return BadRequest("reportType must be provided");

        if (reportType is not ("master" or "regression"))
            return BadRequest("reportType must be 'master' or 'regression'");

        var files = await _driveService.GetFilesAsync(reportType, cancellationToken);

        return Ok(files);
    }

    [HttpGet("known/{name}")]
    public async Task<IActionResult> GetKnownFile(string name, CancellationToken cancellationToken = default)
    {
        if (!_options.KnownFileIds.TryGetValue(name, out var fileId) || string.IsNullOrWhiteSpace(fileId))
            return NotFound($"Known file '{name}' is not configured");

        var file = await _driveService.GetFileAsync(fileId, cancellationToken);
        if (file == null)
        {
            return Ok(new GoogleFileInfo { Id = fileId, Name = name });
        }

        return Ok(file);
    }

    [HttpGet("{fileId}")]
    public async Task<IActionResult> GetFile(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest("fileId must be provided");

        var file = await _driveService.GetFileAsync(fileId, cancellationToken);
        if (file == null) return NotFound();

        return Ok(file);
    }

    [HttpGet("{fileId}/sheets")]
    public async Task<IActionResult> GetSheets(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest("fileId must be provided");

        var sheets = await _sheetsService.GetSheetsAsync(fileId, cancellationToken);
        return Ok(sheets);
    }

    [HttpGet("{fileId}/dashboard-summary")]
    public async Task<IActionResult> GetDashboardSummary(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest("fileId must be provided");

        var summary = await _sheetsService.GetDashboardSummaryAsync(fileId, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("{fileId}/sheets/{sheetName}/rows")]
    public async Task<IActionResult> GetRows(string fileId, string sheetName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            return BadRequest("fileId must be provided");

        if (string.IsNullOrWhiteSpace(sheetName))
            return BadRequest("sheetName must be provided");

        var rows = await _sheetsService.GetRowsAsync(fileId, Uri.UnescapeDataString(sheetName), cancellationToken);
        return Ok(rows);
    }
}
