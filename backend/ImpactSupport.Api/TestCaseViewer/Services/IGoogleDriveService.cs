using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IGoogleDriveService
{
    Task<IReadOnlyList<GoogleFileInfo>> GetFilesAsync(
        string reportType, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GoogleFileInfo>> GetFilesInFolderAsync(
        string folderId, string reportType, CancellationToken cancellationToken = default);

    Task<GoogleFileInfo?> GetFileAsync(
        string fileId, CancellationToken cancellationToken = default);

    
}
