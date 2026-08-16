using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IMasterReviewService
{
    Task<IReadOnlyList<MasterModuleSummaryResponse>> GetModulesAsync(CancellationToken cancellationToken = default);
    Task<MasterTemplateListResponse> GetListAsync(int? moduleId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MasterLookupsResponse> GetLookupsAsync(CancellationToken cancellationToken = default);
    Task<MasterTemplateDetailResponse?> GetDetailAsync(string masterTestId, CancellationToken cancellationToken = default);
    Task<MasterTemplateDetailResponse?> UpdateAsync(string masterTestId, MasterTemplateUpdateRequest request, AuthUser? user, CancellationToken cancellationToken = default);
}
