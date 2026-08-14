namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaDashboardSheetCache
{
    public int Id { get; set; }
    public int FileCacheId { get; set; }
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
    public string RowsJson { get; set; } = string.Empty;
    public DateTimeOffset? LastRefreshedAt { get; set; }
    public string RefreshStatus { get; set; } = "NotStarted";
    public string RefreshError { get; set; } = string.Empty;
    public QaDashboardFileCache? FileCache { get; set; }
}
