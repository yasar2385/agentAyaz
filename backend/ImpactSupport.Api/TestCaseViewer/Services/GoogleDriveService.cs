using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.Extensions.Options;
using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class GoogleDriveService : IGoogleDriveService
{
    private const string SpreadsheetMimeType = "application/vnd.google-apps.spreadsheet";
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    private readonly IGoogleDriveFileLister _fileLister;
    private readonly GoogleOptions _options;

    public GoogleDriveService(IGoogleDriveFileLister fileLister, IOptions<GoogleOptions> options)
    {
        _fileLister = fileLister;
        _options = options.Value;
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

        if (reportType.Equals("regression", StringComparison.OrdinalIgnoreCase))
        {
            return await GetRegressionFilesAsync(folderId, cancellationToken);
        }

        var files = await _fileLister.ListFilesAsync(
            folderId,
            SpreadsheetMimeType,
            "modifiedTime desc",
            cancellationToken);

        return files.Select(ToGoogleFileInfo).ToList();
    }

    public async Task<GoogleFileInfo?> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("fileId must be provided", nameof(fileId));

        var file = await _fileLister.GetFileAsync(fileId, cancellationToken);
        if (file == null) return null;

        return ToGoogleFileInfo(file);
    }

    public async Task<IReadOnlyList<GoogleFileInfo>> GetFilesInFolderAsync(
        string folderId,
        string reportType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderId))
            throw new ArgumentException("folderId must be provided", nameof(folderId));

        if (reportType.Equals("regression", StringComparison.OrdinalIgnoreCase))
        {
            return await GetRegressionFilesAsync(folderId, cancellationToken);
        }

        var files = await _fileLister.ListFilesAsync(
            folderId,
            SpreadsheetMimeType,
            "modifiedTime desc",
            cancellationToken);

        return files.Select(ToGoogleFileInfo).ToList();
    }

    private async Task<IReadOnlyList<GoogleFileInfo>> GetRegressionFilesAsync(
        string folderId,
        CancellationToken cancellationToken)
    {
        var files = new List<GoogleFileInfo>();
        await AddRegressionFilesAsync(folderId, files, cancellationToken);

        return files
            .Where(f => f.Name.StartsWith(_options.RegressionFilePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(f => f.ModifiedTime)
            .ToList();
    }

    private async Task AddRegressionFilesAsync(
        string folderId,
        ICollection<GoogleFileInfo> files,
        CancellationToken cancellationToken)
    {
        var spreadsheetFiles = await _fileLister.ListFilesAsync(
            folderId,
            SpreadsheetMimeType,
            cancellationToken: cancellationToken);

        foreach (var file in spreadsheetFiles)
        {
            files.Add(ToGoogleFileInfo(file));
        }

        var folders = await _fileLister.ListFilesAsync(
            folderId,
            FolderMimeType,
            cancellationToken: cancellationToken);

        foreach (var folder in folders)
        {
            if (!string.IsNullOrWhiteSpace(folder.Id))
            {
                await AddRegressionFilesAsync(folder.Id, files, cancellationToken);
            }
        }
    }

    private static GoogleFileInfo ToGoogleFileInfo(DriveFile file)
    {
        return new GoogleFileInfo
        {
            Id = file.Id,
            Name = file.Name,
            MimeType = file.MimeType,
            ModifiedTime = file.ModifiedTimeDateTimeOffset
        };
    }
}
