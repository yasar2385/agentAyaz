namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaDashboardFileCache
{
    public int Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTimeOffset? LastScannedAt { get; set; }
    public DateTimeOffset? DriveModifiedTime { get; set; }
    public DateTimeOffset? LastDriveCheckedAt { get; set; }
    public DateTimeOffset? LastMetadataSyncedAt { get; set; }
    public DateTimeOffset? LastLocalSyncAt { get; set; }
    public DateTimeOffset? LastGoogleUpdateAt { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string FolderUrl { get; set; } = string.Empty;
    public string ScanStatus { get; set; } = "NotStarted";
    public string ScanError { get; set; } = string.Empty;
    public string LocalTsvPath { get; set; } = string.Empty;
    public int PendingEditCount { get; set; }
    public string SyncStatus { get; set; } = "Local";
    public string SyncError { get; set; } = string.Empty;
    public List<QaDashboardSheetCache> Sheets { get; set; } = [];
}
