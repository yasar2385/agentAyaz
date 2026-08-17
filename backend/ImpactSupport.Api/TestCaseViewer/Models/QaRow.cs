namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class QaRow
{
    public string SourceFileId { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public string SheetName { get; set; } = string.Empty;
    public string TestCaseNo { get; set; } = string.Empty;
    public string TestCaseId { get; set; } = string.Empty;
    public string? OriginalRawTestCaseId { get; set; }
    public string Preconditions { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string RawModule { get; set; } = string.Empty;
    public string ModuleClientPreviewModule { get; set; } = string.Empty;
    public List<string> ModuleClientPreviewClients { get; set; } = [];
    public string ModuleClientPreviewSubClient { get; set; } = string.Empty;
    public string ModuleClientPreviewType { get; set; } = string.Empty;
    public string ModuleClientPreviewDtd { get; set; } = string.Empty;
    public bool PreconditionWildcardWarning { get; set; }
    public string PreconditionWildcardRawValue { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public string PreparedDate { get; set; } = string.Empty;
    public string TestingType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TestCases { get; set; } = string.Empty;
    public string TestData { get; set; } = string.Empty;
    public string ExpectedResult { get; set; } = string.Empty;
    public string ActualResult { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public string QaStatus { get; set; } = string.Empty;
    public string DevStatus { get; set; } = string.Empty;
    public List<QaRound> Rounds { get; set; } = [];
    public List<string> QaRemarks { get; set; } = [];
    public List<string> DevRemarks { get; set; } = [];
}
