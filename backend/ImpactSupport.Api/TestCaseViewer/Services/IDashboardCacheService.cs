using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IDashboardCacheService
{
    Task<DashboardCacheResponse> GetCacheAsync(string reportType, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshFileAsync(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshSheetAsync(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshRegressionIndexAsync(CancellationToken cancellationToken = default);
}
