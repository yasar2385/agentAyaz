namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaDashboardFileCache
{
    public int Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTimeOffset? LastScannedAt { get; set; }
    public string ScanStatus { get; set; } = "NotStarted";
    public string ScanError { get; set; } = string.Empty;
    public List<QaDashboardSheetCache> Sheets { get; set; } = [];
}
