using System.Text.Json;
using Google;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class DashboardCacheService : IDashboardCacheService
{
    private const string MasterReportType = "master";
    private const string RegressionReportType = "regression";
    private const string SuccessStatus = "Success";
    private const string FailedStatus = "Failed";
    private const string RunningStatus = "Running";
    private const string LocalStatus = "Local";
    private const string SyncedStatus = "Synced";
    private const string GoogleNewerStatus = "Google newer";
    private const string PendingEditsStatus = "Pending edits";

    private readonly SupportDbContext _dbContext;
    private readonly IGoogleDriveService _driveService;
    private readonly IGoogleSheetsService _sheetsService;
    private readonly IGoogleDriveUrlParser _urlParser;
    private readonly ITestCaseViewerAccessService _accessService;
    private readonly IQaTsvRowReader _tsvRowReader;
    private readonly TestCaseViewerOptions _options;

    public DashboardCacheService(
        SupportDbContext dbContext,
        IGoogleDriveService driveService,
        IGoogleSheetsService sheetsService,
        IGoogleDriveUrlParser urlParser,
        ITestCaseViewerAccessService accessService,
        IQaTsvRowReader tsvRowReader,
        IOptions<TestCaseViewerOptions> options)
    {
        _dbContext = dbContext;
        _driveService = driveService;
        _sheetsService = sheetsService;
        _urlParser = urlParser;
        _accessService = accessService;
        _tsvRowReader = tsvRowReader;
        _options = options.Value;
    }

    public async Task<DashboardCacheResponse> GetCacheAsync(
        string reportType,
        AuthUser? user = null,
        bool includeOfflineRows = false,
        CancellationToken cancellationToken = default)
    {
        var normalizedReportType = NormalizeReportType(reportType);
        var files = await _dbContext.QaDashboardFileCaches
            .AsNoTracking()
            .Include(file => file.Sheets.OrderBy(sheet => sheet.SheetName))
            .Where(file => file.ReportType == normalizedReportType)
            .OrderBy(file => file.FileName)
            .ToListAsync(cancellationToken);

        return new DashboardCacheResponse
        {
            ReportType = normalizedReportType,
            Files = files
                .Where(file => _accessService.CanSeeFile(user, file))
                .Select(file => ToCachedFile(file, user, includeOfflineRows))
                .Where(file => file.Sheets.Count > 0 || includeOfflineRows || _options.AccessRules.Count == 0)
                .ToList()
        };
    }

    public async Task<DashboardCacheResponse> RefreshFileAsync(
        RefreshDashboardCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        var reportType = NormalizeReportType(request.ReportType);
        var fileId = await ResolveFileIdAsync(request, reportType, cancellationToken);
        var fileName = string.IsNullOrWhiteSpace(request.FileName)
            ? reportType == MasterReportType ? "Testcase_2026" : fileId
            : request.FileName.Trim();
        var fileCache = await UpsertFileAsync(reportType, fileId, fileName, cancellationToken);

        fileCache.ScanStatus = RunningStatus;
        fileCache.ScanError = string.Empty;
        fileCache.SourceUrl = string.IsNullOrWhiteSpace(request.Url) ? BuildSpreadsheetUrl(fileId) : request.Url.Trim();
        fileCache.LastDriveCheckedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            if (reportType == MasterReportType)
            {
                await RefreshMasterIndexAsync(fileCache, cancellationToken);
            }
            else
            {
                var sheets = await _sheetsService.GetSheetsAsync(fileId, cancellationToken);
                foreach (var sheet in sheets)
                {
                    var cachedSheet = UpsertSheet(fileCache, sheet.Name);
                    cachedSheet.SheetIndex = sheet.Index;
                    cachedSheet.SheetGid = sheet.SheetId;
                    cachedSheet.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
                }
            }

            fileCache.ScanStatus = SuccessStatus;
            fileCache.ScanError = string.Empty;
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            fileCache.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
            await WriteFileMirrorAsync(fileCache, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            fileCache.ScanStatus = FailedStatus;
            fileCache.ScanError = ReadGoogleError(ex);
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCacheAsync(reportType, request.User, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> RefreshSheetAsync(
        RefreshDashboardCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        var reportType = NormalizeReportType(request.ReportType);
        if (string.IsNullOrWhiteSpace(request.SheetName))
        {
            throw new ArgumentException("sheetName must be provided", nameof(request));
        }

        var fileId = await ResolveFileIdAsync(request, reportType, cancellationToken);
        var fileName = string.IsNullOrWhiteSpace(request.FileName)
            ? reportType == MasterReportType ? "Testcase_2026" : fileId
            : request.FileName.Trim();
        var fileCache = await UpsertFileAsync(reportType, fileId, fileName, cancellationToken);
        var sheet = UpsertSheet(fileCache, request.SheetName.Trim());

        sheet.RefreshStatus = RunningStatus;
        sheet.RefreshError = string.Empty;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var rowsResponse = await _sheetsService.GetRowsAsync(fileId, sheet.SheetName, cancellationToken);
            ApplyRows(sheet, rowsResponse.Rows);
            sheet.RefreshStatus = SuccessStatus;
            sheet.RefreshError = string.Empty;
            sheet.LastRefreshedAt = DateTimeOffset.UtcNow;
            sheet.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
            await WriteSheetMirrorAsync(fileCache, sheet, rowsResponse.Rows, cancellationToken);
            sheet.RowsJson = string.Empty;
            fileCache.ScanStatus = SuccessStatus;
            fileCache.ScanError = string.Empty;
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            fileCache.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            sheet.RefreshStatus = FailedStatus;
            sheet.RefreshError = ReadGoogleError(ex);
            sheet.LastRefreshedAt = DateTimeOffset.UtcNow;
            fileCache.ScanStatus = FailedStatus;
            fileCache.ScanError = sheet.RefreshError;
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCacheAsync(reportType, request.User, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> RefreshRegressionIndexAsync(
        AuthUser? user = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _driveService.GetFilesAsync(RegressionReportType, cancellationToken);
            foreach (var file in files)
            {
                var cached = await UpsertFileAsync(RegressionReportType, file.Id, file.Name, cancellationToken);
                cached.DriveModifiedTime = file.ModifiedTime;
                cached.SourceUrl = BuildSpreadsheetUrl(file.Id);
                cached.LastDriveCheckedAt = DateTimeOffset.UtcNow;
                cached.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
                cached.ScanStatus = SuccessStatus;
                cached.ScanError = string.Empty;
                cached.LastScannedAt = DateTimeOffset.UtcNow;
                cached.SyncStatus = cached.LastLocalSyncAt.HasValue && file.ModifiedTime > cached.LastLocalSyncAt
                    ? GoogleNewerStatus
                    : LocalStatus;
            }

            await WriteManifestAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var cached = await UpsertFileAsync(RegressionReportType, "regression-index", "Regression index", cancellationToken);
            cached.ScanStatus = FailedStatus;
            cached.ScanError = ReadGoogleError(ex);
            cached.LastScannedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCacheAsync(RegressionReportType, user, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> SyncChangedFilesAsync(
        string reportType,
        AuthUser? user = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedReportType = NormalizeReportType(reportType);
        if (normalizedReportType == RegressionReportType)
        {
            return await RefreshRegressionIndexAsync(user, cancellationToken);
        }

        var fileId = await ResolveFileIdAsync(new RefreshDashboardCacheRequest { ReportType = MasterReportType }, MasterReportType, cancellationToken);
        var fileCache = await UpsertFileAsync(MasterReportType, fileId, "Testcase_2026", cancellationToken);
        if (fileCache.LastLocalSyncAt.HasValue && fileCache.ScanStatus == SuccessStatus)
        {
            fileCache.SyncStatus = LocalStatus;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await GetCacheAsync(MasterReportType, user, false, cancellationToken);
        }

        return await RefreshFileAsync(new RefreshDashboardCacheRequest
        {
            ReportType = MasterReportType,
            FileId = fileId,
            FileName = "Testcase_2026",
            User = user
        }, cancellationToken);
    }

    public async Task<DashboardCacheResponse> ExportTsvAsync(
        string reportType,
        AuthUser? user = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedReportType = NormalizeReportType(reportType);
        var files = await _dbContext.QaDashboardFileCaches
            .Include(file => file.Sheets)
            .Where(file => file.ReportType == normalizedReportType)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            await WriteFileMirrorAsync(file, cancellationToken);
        }

        await WriteManifestAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCacheAsync(normalizedReportType, user, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> SaveChangesAsync(
        RefreshDashboardCacheRequest request,
        CancellationToken cancellationToken = default)
    {
        var reportType = NormalizeReportType(request.ReportType);
        var fileId = await ResolveFileIdAsync(request, reportType, cancellationToken);
        var fileName = string.IsNullOrWhiteSpace(request.FileName)
            ? reportType == MasterReportType ? "Testcase_2026" : fileId
            : request.FileName.Trim();
        var fileCache = await UpsertFileAsync(reportType, fileId, fileName, cancellationToken);
        var allowedEdits = request.Edits
            .Where(edit => IsEditableField(edit.FieldName))
            .ToList();
        var editsBySheet = allowedEdits
            .GroupBy(edit => string.IsNullOrWhiteSpace(edit.SheetName) ? request.SheetName : edit.SheetName);

        try
        {
            foreach (var sheetEdits in editsBySheet)
            {
                var sheetName = sheetEdits.Key;
                var edits = sheetEdits.ToList();
                await _sheetsService.UpdateFieldsAsync(fileId, sheetName, edits, cancellationToken);
                var sheet = UpsertSheet(fileCache, sheetName);
                sheet.PendingEditCount = Math.Max(0, sheet.PendingEditCount - edits.Count);
                sheet.SyncStatus = SyncedStatus;
                sheet.SyncError = string.Empty;
                sheet.LastGoogleUpdateAt = DateTimeOffset.UtcNow;
            }

            fileCache.PendingEditCount = Math.Max(0, fileCache.PendingEditCount - allowedEdits.Count);
            fileCache.SyncStatus = fileCache.PendingEditCount == 0 ? SyncedStatus : PendingEditsStatus;
            fileCache.SyncError = string.Empty;
            fileCache.LastGoogleUpdateAt = DateTimeOffset.UtcNow;
        }
        catch (Exception ex)
        {
            var error = ReadGoogleError(ex);
            foreach (var sheetEdits in editsBySheet)
            {
                var sheet = UpsertSheet(fileCache, sheetEdits.Key);
                sheet.PendingEditCount += sheetEdits.Count();
                sheet.SyncStatus = PendingEditsStatus;
                sheet.SyncError = error;
            }

            fileCache.PendingEditCount += allowedEdits.Count;
            fileCache.SyncStatus = PendingEditsStatus;
            fileCache.SyncError = error;
        }

        await WriteFileMirrorAsync(fileCache, cancellationToken);
        await WriteManifestAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetCacheAsync(reportType, request.User, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> LoadUrlAsync(
        LoadDashboardUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        var parsed = _urlParser.Parse(request.Url);
        var reportType = NormalizeReportType(request.ReportType);

        if (parsed.Kind == GoogleDriveUrlKind.Folder)
        {
            var files = await _driveService.GetFilesInFolderAsync(parsed.Id, reportType, cancellationToken);
            foreach (var file in files)
            {
                var cached = await UpsertFileAsync(reportType, file.Id, file.Name, cancellationToken);
                cached.FolderUrl = parsed.NormalizedUrl;
                cached.SourceUrl = BuildSpreadsheetUrl(file.Id);
                cached.DriveModifiedTime = file.ModifiedTime;
                cached.LastDriveCheckedAt = DateTimeOffset.UtcNow;
                cached.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
                cached.ScanStatus = SuccessStatus;
                cached.ScanError = string.Empty;
                cached.SyncStatus = cached.LastLocalSyncAt.HasValue && file.ModifiedTime > cached.LastLocalSyncAt
                    ? GoogleNewerStatus
                    : LocalStatus;
            }

            await WriteManifestAsync(cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return await GetCacheAsync(reportType, request.User, false, cancellationToken);
        }

        var info = await _driveService.GetFileAsync(parsed.Id, cancellationToken);
        await RefreshFileAsync(new RefreshDashboardCacheRequest
        {
            ReportType = reportType,
            FileId = parsed.Id,
            FileName = info?.Name ?? parsed.Id,
            Url = parsed.NormalizedUrl,
            User = request.User
        }, cancellationToken);

        if (parsed.SheetGid.HasValue)
        {
            var cached = await _dbContext.QaDashboardFileCaches
                .Include(file => file.Sheets)
                .FirstOrDefaultAsync(file => file.ReportType == reportType && file.FileId == parsed.Id, cancellationToken);
            var sheet = cached?.Sheets.FirstOrDefault(item => item.SheetGid == parsed.SheetGid.Value);
            if (cached != null && sheet != null)
            {
                await RefreshSheetAsync(new RefreshDashboardCacheRequest
                {
                    ReportType = reportType,
                    FileId = parsed.Id,
                    FileName = cached.FileName,
                    SheetName = sheet.SheetName,
                    Url = parsed.NormalizedUrl,
                    User = request.User
                }, cancellationToken);
            }
        }

        return await GetCacheAsync(reportType, request.User, false, cancellationToken);
    }

    public async Task<DashboardCacheResponse> DownloadLocalAsync(
        DownloadLocalRequest request,
        CancellationToken cancellationToken = default)
    {
        var reportType = NormalizeReportType(request.ReportType);
        var parsed = _urlParser.Parse(request.Source);

        if (parsed.Kind == GoogleDriveUrlKind.Folder)
        {
            await DownloadFolderToLocalAsync(parsed.Id, parsed.NormalizedUrl, reportType, request.User, cancellationToken);
            return await GetCacheAsync(reportType, request.User, true, cancellationToken);
        }

        try
        {
            await DownloadSpreadsheetToLocalAsync(parsed.Id, parsed.NormalizedUrl, reportType, request.User, cancellationToken);
        }
        catch when (!Uri.TryCreate(request.Source.Trim(), UriKind.Absolute, out _))
        {
            await DownloadFolderToLocalAsync(
                request.Source.Trim(),
                $"https://drive.google.com/drive/folders/{request.Source.Trim()}",
                reportType,
                request.User,
                cancellationToken);
        }

        return await GetCacheAsync(reportType, request.User, true, cancellationToken);
    }

    private async Task DownloadFolderToLocalAsync(
        string folderId,
        string folderUrl,
        string reportType,
        AuthUser? user,
        CancellationToken cancellationToken)
    {
        var files = await _driveService.GetFilesInFolderAsync(folderId, reportType, cancellationToken);
        foreach (var file in files)
        {
            await DownloadSpreadsheetToLocalAsync(file.Id, BuildSpreadsheetUrl(file.Id), reportType, user, cancellationToken, file.Name, file.ModifiedTime, folderUrl);
        }
    }

    private async Task DownloadSpreadsheetToLocalAsync(
        string fileId,
        string sourceUrl,
        string reportType,
        AuthUser? user,
        CancellationToken cancellationToken,
        string? fileName = null,
        DateTimeOffset? modifiedTime = null,
        string folderUrl = "")
    {
        var info = fileName == null ? await _driveService.GetFileAsync(fileId, cancellationToken) : null;
        await RefreshFileAsync(new RefreshDashboardCacheRequest
        {
            ReportType = reportType,
            FileId = fileId,
            FileName = fileName ?? info?.Name ?? fileId,
            Url = sourceUrl,
            User = user
        }, cancellationToken);

        var cached = await _dbContext.QaDashboardFileCaches
            .Include(file => file.Sheets)
            .FirstOrDefaultAsync(file => file.ReportType == reportType && file.FileId == fileId, cancellationToken);
        if (cached == null)
        {
            return;
        }

        cached.FolderUrl = folderUrl;
        cached.DriveModifiedTime = modifiedTime ?? info?.ModifiedTime ?? cached.DriveModifiedTime;
        foreach (var sheet in cached.Sheets.ToList())
        {
            if (!_accessService.CanSeeSheet(user, cached, sheet))
            {
                continue;
            }

            await RefreshSheetAsync(new RefreshDashboardCacheRequest
            {
                ReportType = reportType,
                FileId = fileId,
                FileName = cached.FileName,
                SheetName = sheet.SheetName,
                Url = sourceUrl,
                User = user
            }, cancellationToken);
        }
    }

    private async Task RefreshMasterIndexAsync(QaDashboardFileCache fileCache, CancellationToken cancellationToken)
    {
        var values = await _sheetsService.GetAllValuesAsync(fileCache.FileId, fileCache.FileName, cancellationToken);
        if (values.Count == 0)
        {
            return;
        }

        var headerIndex = FindHeaderRow(values, "Sheet Name");
        if (headerIndex < 0)
        {
            return;
        }

        var headers = values[headerIndex].Select(CellText).ToList();
        var map = BuildHeaderMap(headers);

        for (var i = headerIndex + 1; i < values.Count; i++)
        {
            var row = values[i];
            var sheetName = Get(row, map, "Sheet Name");
            if (string.IsNullOrWhiteSpace(sheetName))
            {
                continue;
            }

            var sheet = UpsertSheet(fileCache, sheetName);
            sheet.Module = Get(row, map, "Purpose of testing");
            sheet.PurposeOfTesting = Get(row, map, "Purpose of testing");
            sheet.TotalTestCases = GetInt(row, map, "Total Testcase count");
            sheet.PassCount = GetInt(row, map, "Pass");
            sheet.FailedCount = GetInt(row, map, "Failed cases");
            sheet.FixedCount = GetInt(row, map, "Fixed");
            sheet.RejectedCount = GetInt(row, map, "Rejected");
            sheet.PostponedCount = GetInt(row, map, "Postponed");
            sheet.NotReplicateCount = GetInt(row, map, "Not Replicate");
            sheet.WipCount = GetInt(row, map, "WIP");
            sheet.NotClearCount = GetInt(row, map, "Not clear");
            sheet.FutureDevelopmentCount = GetInt(row, map, "Future Development") + GetInt(row, map, "Future Development Testcases");
            sheet.DevStatus = Get(row, map, "Dev Status");
            sheet.DevRemarks = Get(row, map, "Dev Remarks");
            sheet.Remarks = Get(row, map, "Remarks");
            sheet.SheetLink = Get(row, map, "Sheet Link");
            sheet.Link = Get(row, map, "Link");
            sheet.RefreshStatus = SuccessStatus;
            sheet.RefreshError = string.Empty;
            sheet.LastRefreshedAt = DateTimeOffset.UtcNow;
            sheet.LastMetadataSyncedAt = DateTimeOffset.UtcNow;
        }
    }

    private async Task<string> ResolveFileIdAsync(
        RefreshDashboardCacheRequest request,
        string reportType,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.FileId))
        {
            return request.FileId.Trim();
        }

        if (reportType == MasterReportType
            && _options.KnownFileIds.TryGetValue("Testcase_2026", out var fileId)
            && !string.IsNullOrWhiteSpace(fileId))
        {
            return fileId;
        }

        var cached = await _dbContext.QaDashboardFileCaches
            .AsNoTracking()
            .Where(file => file.ReportType == reportType)
            .OrderBy(file => file.FileName)
            .FirstOrDefaultAsync(cancellationToken);

        if (cached != null)
        {
            return cached.FileId;
        }

        throw new InvalidOperationException("fileId must be provided");
    }

    private async Task<QaDashboardFileCache> UpsertFileAsync(
        string reportType,
        string fileId,
        string fileName,
        CancellationToken cancellationToken)
    {
        var cached = await _dbContext.QaDashboardFileCaches
            .Include(file => file.Sheets)
            .FirstOrDefaultAsync(file => file.ReportType == reportType && file.FileId == fileId, cancellationToken);

        if (cached != null)
        {
            cached.FileName = fileName;
            return cached;
        }

        cached = new QaDashboardFileCache
        {
            ReportType = reportType,
            FileId = fileId,
            FileName = fileName
        };
        _dbContext.QaDashboardFileCaches.Add(cached);
        return cached;
    }

    private static QaDashboardSheetCache UpsertSheet(QaDashboardFileCache fileCache, string sheetName)
    {
        var cached = fileCache.Sheets.FirstOrDefault(sheet => string.Equals(sheet.SheetName, sheetName, StringComparison.OrdinalIgnoreCase));
        if (cached != null)
        {
            return cached;
        }

        cached = new QaDashboardSheetCache
        {
            FileId = fileCache.FileId,
            SheetName = sheetName
        };
        fileCache.Sheets.Add(cached);
        return cached;
    }

    private static void ApplyRows(QaDashboardSheetCache sheet, IReadOnlyList<QaRow> rows)
    {
        sheet.RowsJson = JsonSerializer.Serialize(rows);
        sheet.TotalTestCases = rows.Count;
        sheet.Module = rows.Select(row => row.Module).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? sheet.Module;
        sheet.PassCount = CountByStatus(rows, "pass");
        sheet.FailedCount = CountByStatus(rows, "fail");
        sheet.FixedCount = CountByStatus(rows, "fixed");
        sheet.RejectedCount = CountByStatus(rows, "reject");
        sheet.PostponedCount = CountByStatus(rows, "postpon");
        sheet.WipCount = CountByStatus(rows, "wip");
        sheet.NotClearCount = CountByStatus(rows, "not clear", "clear");
        sheet.FutureDevelopmentCount = CountByStatus(rows, "future");
        sheet.DevStatus = rows.Select(row => row.DevStatus).LastOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? sheet.DevStatus;
        sheet.DevRemarks = string.Join("; ", rows.SelectMany(row => row.DevRemarks).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct());
    }

    private async Task WriteFileMirrorAsync(QaDashboardFileCache fileCache, CancellationToken cancellationToken)
    {
        var directory = GetReportDirectory(fileCache.ReportType, fileCache.FileName);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{SafeFileName(fileCache.FileName)}__sheets.tsv");
        var lines = new List<string>
        {
            TsvLine([
                "SheetName", "Module", "PurposeOfTesting", "TotalTestCases", "Pass", "Failed", "Fixed", "Rejected",
                "Postponed", "NotReplicate", "WIP", "NotClear", "FutureDevelopment", "DevStatus", "DevRemarks",
                "Remarks", "SheetLink", "Link", "SyncStatus", "PendingEditCount", "LastRefreshedAt"
            ])
        };

        foreach (var sheet in fileCache.Sheets.OrderBy(sheet => sheet.SheetName))
        {
            lines.Add(TsvLine([
                sheet.SheetName,
                sheet.Module,
                sheet.PurposeOfTesting,
                sheet.TotalTestCases.ToString(),
                sheet.PassCount.ToString(),
                sheet.FailedCount.ToString(),
                sheet.FixedCount.ToString(),
                sheet.RejectedCount.ToString(),
                sheet.PostponedCount.ToString(),
                sheet.NotReplicateCount.ToString(),
                sheet.WipCount.ToString(),
                sheet.NotClearCount.ToString(),
                sheet.FutureDevelopmentCount.ToString(),
                sheet.DevStatus,
                sheet.DevRemarks,
                sheet.Remarks,
                sheet.SheetLink,
                sheet.Link,
                sheet.SyncStatus,
                sheet.PendingEditCount.ToString(),
                sheet.LastRefreshedAt?.ToString("O") ?? string.Empty
            ]));
        }

        await File.WriteAllLinesAsync(path, lines, cancellationToken);
        fileCache.LocalTsvPath = path;
        fileCache.LastLocalSyncAt = DateTimeOffset.UtcNow;
        if (fileCache.PendingEditCount == 0)
        {
            fileCache.SyncStatus = SyncedStatus;
        }
        await WriteManifestAsync(cancellationToken);
    }

    private async Task WriteSheetMirrorAsync(
        QaDashboardFileCache fileCache,
        QaDashboardSheetCache sheet,
        IReadOnlyList<QaRow> rows,
        CancellationToken cancellationToken)
    {
        var directory = GetReportDirectory(fileCache.ReportType, fileCache.FileName);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{SafeFileName(sheet.SheetName)}__rows.tsv");
        var lines = new List<string>
        {
            TsvLine([
                "SheetName", "TestCaseNo", "TestCaseId", "Module", "Description", "QAStatus", "DevStatus",
                "IssueType", "ActualResult", "QARemarks", "DevRemarks", "Rounds"
            ])
        };

        foreach (var row in rows)
        {
            lines.Add(TsvLine([
                row.SheetName,
                row.TestCaseNo,
                row.TestCaseId,
                row.Module,
                row.Description,
                row.QaStatus,
                row.DevStatus,
                row.IssueType,
                row.ActualResult,
                string.Join(" | ", row.QaRemarks),
                string.Join(" | ", row.DevRemarks),
                string.Join(" | ", row.Rounds.Select(round => $"R{round.RoundNumber}: QA={round.QaStatus}; Dev={round.DevStatus}"))
            ]));
        }

        await File.WriteAllLinesAsync(path, lines, cancellationToken);
        sheet.LocalTsvPath = path;
        sheet.LastLocalSyncAt = DateTimeOffset.UtcNow;
        if (sheet.PendingEditCount == 0)
        {
            sheet.SyncStatus = SyncedStatus;
            sheet.SyncError = string.Empty;
        }
    }

    private async Task WriteSheetMirrorAsync(
        QaDashboardFileCache fileCache,
        QaDashboardSheetCache sheet,
        CancellationToken cancellationToken)
    {
        var rows = string.IsNullOrWhiteSpace(sheet.RowsJson)
            ? _tsvRowReader.ReadRows(sheet.LocalTsvPath)
            : JsonSerializer.Deserialize<IReadOnlyList<QaRow>>(sheet.RowsJson) ?? [];
        await WriteSheetMirrorAsync(fileCache, sheet, rows, cancellationToken);
    }

    private async Task WriteManifestAsync(CancellationToken cancellationToken)
    {
        var directory = GetBaseDirectory();
        Directory.CreateDirectory(directory);
        var files = await _dbContext.QaDashboardFileCaches
            .AsNoTracking()
            .Include(file => file.Sheets)
            .OrderBy(file => file.ReportType)
            .ThenBy(file => file.FileName)
            .ToListAsync(cancellationToken);
        var manifest = files.Select(file => new
        {
            file.FileId,
            file.FileName,
            file.ReportType,
            file.DriveModifiedTime,
            file.LocalTsvPath,
            file.LastLocalSyncAt,
            file.LastGoogleUpdateAt,
            file.PendingEditCount,
            file.SyncStatus,
            file.SyncError,
            Sheets = file.Sheets.Select(sheet => new
            {
                sheet.SheetName,
                sheet.LocalTsvPath,
                sheet.LastLocalSyncAt,
                sheet.LastGoogleUpdateAt,
                sheet.PendingEditCount,
                sheet.SyncStatus,
                sheet.SyncError
            })
        });

        await File.WriteAllTextAsync(
            Path.Combine(directory, "sync-manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }),
            cancellationToken);
    }

    private static bool IsEditableField(string fieldName)
    {
        return fieldName.Equals("QA Status", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("Dev. Status", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("QA Remarks", StringComparison.OrdinalIgnoreCase)
            || fieldName.Equals("Dev. Remarks", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("remark", StringComparison.OrdinalIgnoreCase)
            || fieldName.Contains("provision", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetReportDirectory(string reportType, string fileName)
    {
        return Path.Combine(
            GetBaseDirectory(),
            reportType == RegressionReportType ? "Regression" : "Testcase_2026",
            SafeFileName(fileName));
    }

    private static string GetBaseDirectory()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        return Path.Combine(documents, "ImpactSupport", "TestCaseViewer");
    }

    private static string SafeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "untitled" : safe;
    }

    private static string BuildSpreadsheetUrl(string fileId)
    {
        return string.IsNullOrWhiteSpace(fileId)
            ? string.Empty
            : $"https://docs.google.com/spreadsheets/d/{fileId}/edit";
    }

    private static string TsvLine(IEnumerable<string> values)
    {
        return string.Join('\t', values.Select(TsvCell));
    }

    private static string TsvCell(string value)
    {
        return value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ')
            .Trim();
    }

    private static int CountByStatus(IEnumerable<QaRow> rows, params string[] terms)
    {
        return rows.Count(row =>
        {
            var value = $"{row.QaStatus} {row.DevStatus} {row.IssueType} {row.ActualResult}".ToLowerInvariant();
            return terms.Any(value.Contains);
        });
    }

    private DashboardCachedFile ToCachedFile(QaDashboardFileCache file, AuthUser? user, bool includeOfflineRows)
    {
        return new DashboardCachedFile
        {
            FileId = file.FileId,
            FileName = file.FileName,
            ReportType = file.ReportType,
            LastScannedAt = file.LastScannedAt,
            DriveModifiedTime = file.DriveModifiedTime,
            LastDriveCheckedAt = file.LastDriveCheckedAt,
            LastMetadataSyncedAt = file.LastMetadataSyncedAt,
            LastLocalSyncAt = file.LastLocalSyncAt,
            LastGoogleUpdateAt = file.LastGoogleUpdateAt,
            SourceUrl = file.SourceUrl,
            FolderUrl = file.FolderUrl,
            ScanStatus = file.ScanStatus,
            ScanError = file.ScanError,
            LocalTsvPath = file.LocalTsvPath,
            PendingEditCount = file.PendingEditCount,
            SyncStatus = file.SyncStatus,
            SyncError = file.SyncError,
            Sheets = file.Sheets
                .Where(sheet => _accessService.CanSeeSheet(user, file, sheet))
                .Select(sheet => ToCachedSheet(sheet, includeOfflineRows))
                .ToList()
        };
    }

    private DashboardCachedSheet ToCachedSheet(QaDashboardSheetCache sheet, bool includeOfflineRows)
    {
        var rows = ReadRowsForResponse(sheet, includeOfflineRows);
        return new DashboardCachedSheet
        {
            FileId = sheet.FileId,
            SheetName = sheet.SheetName,
            SheetIndex = sheet.SheetIndex,
            SheetGid = sheet.SheetGid,
            Module = sheet.Module,
            TotalTestCases = sheet.TotalTestCases,
            PassCount = sheet.PassCount,
            FailedCount = sheet.FailedCount,
            FixedCount = sheet.FixedCount,
            RejectedCount = sheet.RejectedCount,
            PostponedCount = sheet.PostponedCount,
            NotReplicateCount = sheet.NotReplicateCount,
            WipCount = sheet.WipCount,
            NotClearCount = sheet.NotClearCount,
            FutureDevelopmentCount = sheet.FutureDevelopmentCount,
            PurposeOfTesting = sheet.PurposeOfTesting,
            DevStatus = sheet.DevStatus,
            DevRemarks = sheet.DevRemarks,
            Remarks = sheet.Remarks,
            SheetLink = sheet.SheetLink,
            Link = sheet.Link,
            LastRefreshedAt = sheet.LastRefreshedAt,
            DriveModifiedTime = sheet.DriveModifiedTime,
            LastMetadataSyncedAt = sheet.LastMetadataSyncedAt,
            LastLocalSyncAt = sheet.LastLocalSyncAt,
            LastGoogleUpdateAt = sheet.LastGoogleUpdateAt,
            LocalTsvPath = sheet.LocalTsvPath,
            PendingEditCount = sheet.PendingEditCount,
            SyncStatus = sheet.SyncStatus,
            SyncError = includeOfflineRows && !File.Exists(sheet.LocalTsvPath)
                ? "Local source not available. Download to Local first."
                : sheet.SyncError,
            RefreshStatus = sheet.RefreshStatus,
            RefreshError = sheet.RefreshError,
            Rows = rows
        };
    }

    private IReadOnlyList<QaRow> ReadRowsForResponse(QaDashboardSheetCache sheet, bool includeOfflineRows)
    {
        if (includeOfflineRows)
        {
            return _tsvRowReader.ReadRows(sheet.LocalTsvPath);
        }

        return string.IsNullOrWhiteSpace(sheet.RowsJson)
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<QaRow>>(sheet.RowsJson) ?? [];
    }

    private static int FindHeaderRow(IList<IList<object>> values, string requiredHeader)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].Any(cell => string.Equals(CellText(cell), requiredHeader, StringComparison.OrdinalIgnoreCase)))
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < header.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(header[i]) && !map.ContainsKey(header[i]))
            {
                map[header[i]] = i;
            }
        }

        return map;
    }

    private static string Get(IList<object> row, IReadOnlyDictionary<string, int> map, string header)
    {
        return map.TryGetValue(header, out var index) && index < row.Count ? CellText(row[index]) : string.Empty;
    }

    private static int GetInt(IList<object> row, IReadOnlyDictionary<string, int> map, string header)
    {
        return int.TryParse(Get(row, map, header), out var value) ? value : 0;
    }

    private static string NormalizeReportType(string reportType)
    {
        return reportType.Equals(RegressionReportType, StringComparison.OrdinalIgnoreCase)
            ? RegressionReportType
            : MasterReportType;
    }

    private static string ReadGoogleError(Exception ex)
    {
        return ex is GoogleApiException googleException ? googleException.Error?.Message ?? googleException.Message : ex.Message;
    }

    private static string CellText(object? value) => value?.ToString()?.Trim() ?? string.Empty;
}
