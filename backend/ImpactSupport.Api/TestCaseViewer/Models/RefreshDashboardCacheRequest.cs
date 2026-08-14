namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class RefreshDashboardCacheRequest
{
    public string ReportType { get; set; } = "master";
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
}
