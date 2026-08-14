using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IDashboardCacheService
{
    Task<DashboardCacheResponse> GetCacheAsync(string reportType, AuthUser? user = null, bool includeOfflineRows = false, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshFileAsync(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshSheetAsync(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> RefreshRegressionIndexAsync(AuthUser? user = null, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> SyncChangedFilesAsync(string reportType, AuthUser? user = null, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> ExportTsvAsync(string reportType, AuthUser? user = null, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> SaveChangesAsync(RefreshDashboardCacheRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> LoadUrlAsync(LoadDashboardUrlRequest request, CancellationToken cancellationToken = default);
    Task<DashboardCacheResponse> DownloadLocalAsync(DownloadLocalRequest request, CancellationToken cancellationToken = default);
}
