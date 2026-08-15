namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaImportBatchError
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }
    public int RowNumber { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public QaImportBatch? ImportBatch { get; set; }
}
