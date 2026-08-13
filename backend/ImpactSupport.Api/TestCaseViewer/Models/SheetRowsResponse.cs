namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class SheetRowsResponse
{
    public string FileId { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public IReadOnlyList<QaRow> Rows { get; set; } = [];
    public IReadOnlyList<string> QaStatuses { get; set; } = [];
    public IReadOnlyList<string> DevStatuses { get; set; } = [];
}
