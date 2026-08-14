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

    private readonly SupportDbContext _dbContext;
    private readonly IGoogleDriveService _driveService;
    private readonly IGoogleSheetsService _sheetsService;
    private readonly TestCaseViewerOptions _options;

    public DashboardCacheService(
        SupportDbContext dbContext,
        IGoogleDriveService driveService,
        IGoogleSheetsService sheetsService,
        IOptions<TestCaseViewerOptions> options)
    {
        _dbContext = dbContext;
        _driveService = driveService;
        _sheetsService = sheetsService;
        _options = options.Value;
    }

    public async Task<DashboardCacheResponse> GetCacheAsync(string reportType, CancellationToken cancellationToken = default)
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
            Files = files.Select(ToCachedFile).ToList()
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
                    UpsertSheet(fileCache, sheet.Name);
                }
            }

            fileCache.ScanStatus = SuccessStatus;
            fileCache.ScanError = string.Empty;
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            fileCache.ScanStatus = FailedStatus;
            fileCache.ScanError = ReadGoogleError(ex);
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetCacheAsync(reportType, cancellationToken);
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
            fileCache.ScanStatus = SuccessStatus;
            fileCache.ScanError = string.Empty;
            fileCache.LastScannedAt = DateTimeOffset.UtcNow;
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

        return await GetCacheAsync(reportType, cancellationToken);
    }

    public async Task<DashboardCacheResponse> RefreshRegressionIndexAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var files = await _driveService.GetFilesAsync(RegressionReportType, cancellationToken);
            foreach (var file in files)
            {
                var cached = await UpsertFileAsync(RegressionReportType, file.Id, file.Name, cancellationToken);
                cached.ScanStatus = SuccessStatus;
                cached.ScanError = string.Empty;
                cached.LastScannedAt = DateTimeOffset.UtcNow;
            }

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

        return await GetCacheAsync(RegressionReportType, cancellationToken);
    }

    private async Task RefreshMasterIndexAsync(QaDashboardFileCache fileCache, CancellationToken cancellationToken)
    {
        var values = await _sheetsService.GetValuesAsync(fileCache.FileId, fileCache.FileName, cancellationToken);
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

    private static int CountByStatus(IEnumerable<QaRow> rows, params string[] terms)
    {
        return rows.Count(row =>
        {
            var value = $"{row.QaStatus} {row.DevStatus} {row.IssueType} {row.ActualResult}".ToLowerInvariant();
            return terms.Any(value.Contains);
        });
    }

    private static DashboardCachedFile ToCachedFile(QaDashboardFileCache file)
    {
        return new DashboardCachedFile
        {
            FileId = file.FileId,
            FileName = file.FileName,
            ReportType = file.ReportType,
            LastScannedAt = file.LastScannedAt,
            ScanStatus = file.ScanStatus,
            ScanError = file.ScanError,
            Sheets = file.Sheets.Select(ToCachedSheet).ToList()
        };
    }

    private static DashboardCachedSheet ToCachedSheet(QaDashboardSheetCache sheet)
    {
        return new DashboardCachedSheet
        {
            FileId = sheet.FileId,
            SheetName = sheet.SheetName,
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
            RefreshStatus = sheet.RefreshStatus,
            RefreshError = sheet.RefreshError,
            Rows = string.IsNullOrWhiteSpace(sheet.RowsJson)
                ? []
                : JsonSerializer.Deserialize<IReadOnlyList<QaRow>>(sheet.RowsJson) ?? []
        };
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
