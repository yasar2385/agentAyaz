namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class ImportBatchResponse
{
    public int BatchId { get; set; }
    public string UploadKind { get; set; } = string.Empty;
    public string ResultMode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RowsAdded { get; set; }
    public int RowsUpdated { get; set; }
    public int RowsSkipped { get; set; }
    public int RowsError { get; set; }
    public int SheetsDetected { get; set; }
    public int NewSheets { get; set; }
    public int ExistingSheets { get; set; }
    public IReadOnlyList<ImportBatchSheetResponse> Sheets { get; set; } = [];
    public IReadOnlyList<ImportBatchErrorResponse> Errors { get; set; } = [];
}

public sealed class ImportBatchSheetResponse
{
    public int Id { get; set; }
    public string SheetName { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public string ConflictStatus { get; set; } = string.Empty;
    public string SelectedAction { get; set; } = string.Empty;
}

public sealed class ImportBatchErrorResponse
{
    public int Id { get; set; }
    public int RowNumber { get; set; }
    public string RawValue { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public sealed class SheetActionRequest
{
    public IReadOnlyList<SheetActionItem> Actions { get; set; } = [];
}

public sealed class SheetActionItem
{
    public int SheetId { get; set; }
    public string Action { get; set; } = string.Empty;
}
