namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class SheetSummary
{
    public string SheetName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public int TotalTestCases { get; set; }
    public Dictionary<string, int> QaStatusCounts { get; set; } = [];
    public Dictionary<string, int> DevStatusCounts { get; set; } = [];
}
