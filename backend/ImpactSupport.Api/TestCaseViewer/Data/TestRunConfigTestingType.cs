namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestRunConfigTestingType
{
    public int ConfigId { get; set; }
    public string Value { get; set; } = string.Empty;
    public TestRunConfig? Config { get; set; }
}
