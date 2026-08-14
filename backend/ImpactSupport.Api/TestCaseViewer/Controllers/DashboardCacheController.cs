using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImpactSupport.Api.TestCaseViewer.Controllers;

[ApiController]
[Route("api/testcaseviewer/dashboard-cache")]
public sealed class DashboardCacheController : ControllerBase
{
    private readonly IDashboardCacheService _cacheService;

    public DashboardCacheController(IDashboardCacheService cacheService)
    {
        _cacheService = cacheService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCache([FromQuery] string reportType = "master", CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.GetCacheAsync(reportType, ReadUser(), false, cancellationToken));
    }

    [HttpGet("offline")]
    public async Task<IActionResult> GetOfflineCache([FromQuery] string reportType = "master", CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.GetCacheAsync(reportType, ReadUser(), true, cancellationToken));
    }

    [HttpPost("refresh-file")]
    public async Task<IActionResult> RefreshFile(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default)
    {
        request.User ??= ReadUser();
        return Ok(await _cacheService.RefreshFileAsync(request, cancellationToken));
    }

    [HttpPost("refresh-sheet")]
    public async Task<IActionResult> RefreshSheet(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default)
    {
        request.User ??= ReadUser();
        return Ok(await _cacheService.RefreshSheetAsync(request, cancellationToken));
    }

    [HttpPost("refresh-regression-index")]
    public async Task<IActionResult> RefreshRegressionIndex(CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.RefreshRegressionIndexAsync(ReadUser(), cancellationToken));
    }

    [HttpPost("sync-changed-files")]
    public async Task<IActionResult> SyncChangedFiles([FromQuery] string reportType = "master", CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.SyncChangedFilesAsync(reportType, ReadUser(), cancellationToken));
    }

    [HttpPost("export-tsv")]
    public async Task<IActionResult> ExportTsv([FromQuery] string reportType = "master", CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.ExportTsvAsync(reportType, ReadUser(), cancellationToken));
    }

    [HttpPost("save-changes")]
    public async Task<IActionResult> SaveChanges(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default)
    {
        request.User ??= ReadUser();
        return Ok(await _cacheService.SaveChangesAsync(request, cancellationToken));
    }

    [HttpPost("load-url")]
    public async Task<IActionResult> LoadUrl(LoadDashboardUrlRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("url must be provided");

        request.User ??= ReadUser();
        return Ok(await _cacheService.LoadUrlAsync(request, cancellationToken));
    }

    [HttpPost("download-local")]
    public async Task<IActionResult> DownloadLocal(DownloadLocalRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Source))
            return BadRequest("source must be provided");

        request.User ??= ReadUser();
        return Ok(await _cacheService.DownloadLocalAsync(request, cancellationToken));
    }

    private AuthUser? ReadUser()
    {
        var username = Request.Headers["X-TestCaseViewer-Username"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        return new AuthUser
        {
            Id = Request.Headers["X-TestCaseViewer-UserId"].FirstOrDefault() ?? string.Empty,
            Username = username,
            DisplayName = Request.Headers["X-TestCaseViewer-DisplayName"].FirstOrDefault() ?? username,
            Role = Request.Headers["X-TestCaseViewer-Role"].FirstOrDefault() ?? string.Empty,
            Email = Request.Headers["X-TestCaseViewer-Email"].FirstOrDefault() ?? string.Empty
        };
    }
}
