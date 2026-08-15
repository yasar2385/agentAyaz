namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunConfigWorkflowContext
{
    public int ConfigId { get; set; }
    public string Client { get; set; } = "ALL";
    public string ContentType { get; set; } = "books";
    public string Domain { get; set; } = "UAT";
    public string RoleWorkflow { get; set; } = "author_editor_collator";
    public string TestingUrl { get; set; } = "author";
    public string MantisTicket { get; set; } = string.Empty;
    public string RefStyle { get; set; } = "number";
    public TestRunConfig? Config { get; set; }
}
