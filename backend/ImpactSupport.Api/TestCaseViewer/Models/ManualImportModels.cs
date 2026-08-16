namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class ImportBatchResponse
{
    public int BatchId { get; set; }
    public string UploadKind { get; set; } = string.Empty;
    public string ResultMode { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
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
    public IReadOnlyList<DuplicateIdResolutionResponse> DuplicateIdsResolved { get; set; } = [];
    public IReadOnlyList<ManualEditConflictResponse> ManualEditConflicts { get; set; } = [];
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

public sealed class DuplicateIdResolutionResponse
{
    public string RawId { get; set; } = string.Empty;
    public string ResolvedId { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
}

public sealed class ManualEditConflictResponse
{
    public int RowId { get; set; }
    public string MasterTestId { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public int SourceRowNumber { get; set; }
    public string LastEditedBy { get; set; } = string.Empty;
    public DateTimeOffset? LastEditedAt { get; set; }
    public string SelectedAction { get; set; } = string.Empty;
}

public sealed class ImportInspectResponse
{
    public string UploadToken { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public IReadOnlyList<ImportInspectSheetResponse> Sheets { get; set; } = [];
}

public sealed class ImportInspectSheetResponse
{
    public string SheetName { get; set; } = string.Empty;
    public string Visibility { get; set; } = "visible";
    public int RowCountEstimate { get; set; }
}

public sealed class ParseMasterImportRequest
{
    public string UploadToken { get; set; } = string.Empty;
    public IReadOnlyList<string> SheetNames { get; set; } = [];
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

public sealed class ManualEditActionRequest
{
    public IReadOnlyList<ManualEditActionItem> Actions { get; set; } = [];
}

public sealed class ManualEditActionItem
{
    public int RowId { get; set; }
    public string Action { get; set; } = string.Empty;
}
