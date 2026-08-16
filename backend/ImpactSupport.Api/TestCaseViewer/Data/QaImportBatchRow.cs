namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaImportBatchRow
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public int ImportBatchSheetId { get; set; }
    public int SourceRowNumber { get; set; }
    public string TestCaseId { get; set; } = string.Empty;
    public string? OriginalRawTestCaseId { get; set; }
    public bool ManualEditConflict { get; set; }
    public string ManualEditAction { get; set; } = string.Empty;
    public string ManualEditLastEditedBy { get; set; } = string.Empty;
    public DateTimeOffset? ManualEditLastEditedAt { get; set; }
    public string RowJson { get; set; } = string.Empty;
    public QaImportBatch? ImportBatch { get; set; }
    public QaImportBatchSheet? ImportBatchSheet { get; set; }
}
