using Google.Apis.Drive.v3;
using Google.Apis.Services;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleDriveFileLister : IGoogleDriveFileLister
{
    private readonly DriveService _driveService;

    public GoogleDriveFileLister(IGoogleCredentialProvider credentialProvider)
    {
        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credentialProvider.GetCredential(),
            ApplicationName = "ImpactSupport.TestCaseViewer"
        });
    }

    public async Task<IReadOnlyList<DriveFile>> ListFilesAsync(
        string folderId,
        string mimeType,
        string? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        var request = _driveService.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false and mimeType = '{mimeType}'";
        request.Fields = "files(id, name, mimeType, modifiedTime)";
        request.OrderBy = orderBy;

        var response = await request.ExecuteAsync(cancellationToken);
        return response.Files.ToList();
    }

    public async Task<DriveFile?> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var request = _driveService.Files.Get(fileId);
        request.Fields = "id,name,mimeType,modifiedTime";

        return await request.ExecuteAsync(cancellationToken);
    }
}
