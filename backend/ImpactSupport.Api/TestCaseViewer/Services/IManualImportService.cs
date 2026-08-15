using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IManualImportService
{
    Task<ImportBatchResponse> UploadMasterAsync(IFormFile file, AuthUser? user, CancellationToken cancellationToken = default);
    Task<ImportBatchResponse> UploadResultsAsync(IReadOnlyList<IFormFile> files, string resultMode, AuthUser? user, CancellationToken cancellationToken = default);
    Task<ImportBatchResponse?> GetBatchAsync(int batchId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImportBatchErrorResponse>> GetErrorsAsync(int batchId, CancellationToken cancellationToken = default);
    Task<ImportBatchResponse?> SaveSheetActionsAsync(int batchId, SheetActionRequest request, CancellationToken cancellationToken = default);
    Task<ImportBatchResponse?> CommitAsync(int batchId, CancellationToken cancellationToken = default);
}
