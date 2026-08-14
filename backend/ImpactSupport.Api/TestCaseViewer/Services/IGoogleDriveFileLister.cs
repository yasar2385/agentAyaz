using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IGoogleDriveFileLister
{
    Task<IReadOnlyList<DriveFile>> ListFilesAsync(
        string folderId,
        string mimeType,
        string? orderBy = null,
        CancellationToken cancellationToken = default);

    Task<DriveFile?> GetFileAsync(string fileId, CancellationToken cancellationToken = default);
}
