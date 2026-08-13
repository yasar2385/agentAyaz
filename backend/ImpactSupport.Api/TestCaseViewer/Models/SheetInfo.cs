namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class SheetInfo
{
    public string Name { get; set; } = string.Empty;
    public int? Index { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
}
