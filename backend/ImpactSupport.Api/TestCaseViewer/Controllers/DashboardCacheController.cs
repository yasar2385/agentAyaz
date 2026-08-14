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
        return Ok(await _cacheService.GetCacheAsync(reportType, cancellationToken));
    }

    [HttpPost("refresh-file")]
    public async Task<IActionResult> RefreshFile(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.RefreshFileAsync(request, cancellationToken));
    }

    [HttpPost("refresh-sheet")]
    public async Task<IActionResult> RefreshSheet(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.RefreshSheetAsync(request, cancellationToken));
    }

    [HttpPost("refresh-regression-index")]
    public async Task<IActionResult> RefreshRegressionIndex(CancellationToken cancellationToken = default)
    {
        return Ok(await _cacheService.RefreshRegressionIndexAsync(cancellationToken));
    }
}
