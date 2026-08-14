namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class RefreshDashboardCacheRequest
{
    public string ReportType { get; set; } = "master";
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public AuthUser? User { get; set; }
    public IReadOnlyList<QaFieldEdit> Edits { get; set; } = [];
}

public sealed class LoadDashboardUrlRequest
{
    public string Url { get; set; } = string.Empty;
    public string ReportType { get; set; } = "master";
    public AuthUser? User { get; set; }
}

public sealed class DownloadLocalRequest
{
    public string Source { get; set; } = string.Empty;
    public string ReportType { get; set; } = "master";
    public string DownloadScope { get; set; } = "allSheets";
    public AuthUser? User { get; set; }
}

public sealed class QaFieldEdit
{
    public string SheetName { get; set; } = string.Empty;
    public string TestCaseId { get; set; } = string.Empty;
    public string TestCaseNo { get; set; } = string.Empty;
    public string FieldName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
