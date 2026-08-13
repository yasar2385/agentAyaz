using Google.Apis.Drive.v3;
using Google.Apis.Services;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.Extensions.Options;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleDriveService : IGoogleDriveService
{
    private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";

    private readonly DriveService _driveService;
    private readonly GoogleOptions _options;

    public GoogleDriveService(IGoogleCredentialProvider credentialProvider, IOptions<GoogleOptions> options){
        _options = options.Value;

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credentialProvider.GetCredential(),
            ApplicationName = "ImpactSupport.TestCaseViewer"
        });
    }

    public async Task<IReadOnlyList<GoogleFileInfo>> GetFilesAsync(
        string reportType,
        CancellationToken cancellationToken = default)
    {
        var folderId = reportType.ToLowerInvariant() switch
        {
            "master" => _options.MasterFolderId,
            "regression" => _options.RegressionFolderId,
            _ => throw new ArgumentException($"Invalid report type: {reportType}", nameof(reportType))
        };

        if (string.IsNullOrWhiteSpace(folderId))
        {
            throw new InvalidOperationException(
                $"No folder configured for report type '{reportType}'.");
        }

        var request = _driveService.Files.List();
        request.Q = $"'{folderId}' in parents and trashed = false and mimeType = '{SpreadsheetMimeType}'";
        request.Fields = "files(id, name, mimeType, modifiedTime)";
        request.OrderBy = "modifiedTime desc";

        var response = await request.ExecuteAsync(cancellationToken);

        var files = response.Files
            .Select(f => new GoogleFileInfo
            {
                Id = f.Id,
                Name = f.Name,
                MimeType = f.MimeType,
                ModifiedTime = f.ModifiedTimeDateTimeOffset
            })
            .ToList();

        if (reportType.Equals("regression", StringComparison.OrdinalIgnoreCase))
        {
            files = files
                .Where(f => f.Name.StartsWith(_options.RegressionFilePrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return files;
    }

    public async Task<GoogleFileInfo?> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("fileId must be provided", nameof(fileId));

        var request = _driveService.Files.Get(fileId);
        request.Fields = "id,name,mimeType,modifiedTime";

        var file = await request.ExecuteAsync(cancellationToken);
        if (file == null) return null;

        return new GoogleFileInfo
        {
            Id = file.Id,
            Name = file.Name,
            MimeType = file.MimeType,
            ModifiedTime = file.ModifiedTimeDateTimeOffset
        };
    }



}