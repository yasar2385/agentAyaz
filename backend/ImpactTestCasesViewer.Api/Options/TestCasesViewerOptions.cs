namespace ImpactTestCasesViewer.Api.Options;

public sealed class TestCasesViewerOptions
{
	public string[] DefaultSheetNames { get; set; } = ["Testcase_2026", "Testcase_2025"];

	public int AutoRefreshMinutes { get; set; } = 5;
}