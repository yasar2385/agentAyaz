namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunExecution
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public string TriggeredBy { get; set; } = string.Empty;
    public DateTimeOffset TriggeredAt { get; set; } = DateTimeOffset.UtcNow;
    public string Status { get; set; } = "QUEUED";
    public DateTimeOffset? FinishedAt { get; set; }
    public string PlaywrightCommand { get; set; } = string.Empty;
    public string PlaywrightTestsRef { get; set; } = string.Empty;
    public string ReportPath { get; set; } = string.Empty;
    public string RunKind { get; set; } = "STANDARD";
    public string ModuleName { get; set; } = string.Empty;
    public string TestCaseId { get; set; } = string.Empty;
    public string MantisTicket { get; set; } = string.Empty;
    public int? ExitCode { get; set; }
    public string FailureSummary { get; set; } = string.Empty;
    public TestRunConfig? Config { get; set; }
}
