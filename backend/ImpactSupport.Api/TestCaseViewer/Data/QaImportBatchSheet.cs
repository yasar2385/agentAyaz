namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaImportBatchSheet
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public string NormalizedSheetName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string ConflictStatus { get; set; } = "NEW";
    public string SelectedAction { get; set; } = string.Empty;
    public QaImportBatch? ImportBatch { get; set; }
    public List<QaImportBatchRow> Rows { get; set; } = [];
}
