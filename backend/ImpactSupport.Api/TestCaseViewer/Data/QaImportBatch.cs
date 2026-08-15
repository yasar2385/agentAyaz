namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class QaImportBatch
{
    public int Id { get; set; }
    public string UploadKind { get; set; } = string.Empty;
    public string ResultMode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string Status { get; set; } = "DRY_RUN";
    public int RowsAdded { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsError { get; set; }
    public int SheetsDetected { get; set; }
    public int NewSheets { get; set; }
    public int ExistingSheets { get; set; }
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CommittedAt { get; set; }
    public List<QaImportBatchSheet> Sheets { get; set; } = [];
    public List<QaImportBatchRow> Rows { get; set; } = [];
    public List<QaImportBatchError> Errors { get; set; } = [];
}
