namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunConfigFlag
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public string FlagKey { get; set; } = string.Empty;
    public string FlagValue { get; set; } = string.Empty;
    public TestRunConfig? Config { get; set; }
}
