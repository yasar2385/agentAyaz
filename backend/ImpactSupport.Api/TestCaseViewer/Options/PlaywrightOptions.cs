namespace ImpactSupport.Api.TestCaseViewer.Options;

public sealed class PlaywrightOptions
{
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Command { get; set; } = "npx";
    public string ArgumentsPrefix { get; set; } = "playwright test";
    public string ReportRelativePath { get; set; } = "playwright-report/index.html";
    public int ExecutionTimeoutMinutes { get; set; } = 30;
}
