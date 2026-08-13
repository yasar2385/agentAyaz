namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class DashboardSummary
{
    public string FileId { get; set; } = string.Empty;
    public int TotalSheets { get; set; }
    public int TotalTestCases { get; set; }
    public Dictionary<string, int> QaStatusCounts { get; set; } = [];
    public Dictionary<string, int> DevStatusCounts { get; set; } = [];
    public List<SheetSummary> Sheets { get; set; } = [];
}
