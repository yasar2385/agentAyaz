namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunProgress
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string LastModuleName { get; set; } = string.Empty;
    public int? LastExecutionId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public TestRunConfig? Config { get; set; }
    public TestRunExecution? LastExecution { get; set; }
}
