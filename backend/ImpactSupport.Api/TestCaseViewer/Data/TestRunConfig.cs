namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunConfig
{
    public int Id { get; set; }
    public string TestingName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;
    public List<TestRunConfigTarget> Targets { get; set; } = [];
    public List<TestRunConfigTestingType> TestingTypes { get; set; } = [];
    public List<TestRunConfigFlag> Flags { get; set; } = [];
    public List<TestRunExecution> Executions { get; set; } = [];
    public TestRunConfigWorkflowContext? WorkflowContext { get; set; }
}
