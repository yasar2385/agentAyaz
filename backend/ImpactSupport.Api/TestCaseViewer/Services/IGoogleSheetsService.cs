using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IGoogleSheetsService
{
    Task<IReadOnlyList<SheetInfo>> GetSheetsAsync(string fileId, CancellationToken cancellationToken = default);
    Task<IList<IList<object>>> GetValuesAsync(string fileId, string sheetName, CancellationToken cancellationToken = default);
    Task<SheetRowsResponse> GetRowsAsync(string fileId, string sheetName, CancellationToken cancellationToken = default);
    Task<DashboardSummary> GetDashboardSummaryAsync(string fileId, CancellationToken cancellationToken = default);
}
