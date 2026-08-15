namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunConfigTarget
{
    public int ConfigId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public TestRunConfig? Config { get; set; }
}
