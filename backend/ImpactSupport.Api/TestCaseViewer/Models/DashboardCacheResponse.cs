namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class DashboardCacheResponse
{
    public string ReportType { get; set; } = string.Empty;
    public IReadOnlyList<DashboardCachedFile> Files { get; set; } = [];
}

public sealed class DashboardCachedFile
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTimeOffset? LastScannedAt { get; set; }
    public string ScanStatus { get; set; } = string.Empty;
    public string ScanError { get; set; } = string.Empty;
    public IReadOnlyList<DashboardCachedSheet> Sheets { get; set; } = [];
}

public sealed class DashboardCachedSheet
{
    public string FileId { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public int PassCount { get; set; }
    public int FailedCount { get; set; }
    public int FixedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PostponedCount { get; set; }
    public int NotReplicateCount { get; set; }
    public int WipCount { get; set; }
    public int NotClearCount { get; set; }
    public int FutureDevelopmentCount { get; set; }
    public string PurposeOfTesting { get; set; } = string.Empty;
    public string DevStatus { get; set; } = string.Empty;
    public string DevRemarks { get; set; } = string.Empty;
    public string Remarks { get; set; } = string.Empty;
    public string SheetLink { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTimeOffset? LastRefreshedAt { get; set; }
    public string RefreshStatus { get; set; } = string.Empty;
    public string RefreshError { get; set; } = string.Empty;
    public IReadOnlyList<QaRow> Rows { get; set; } = [];
}
