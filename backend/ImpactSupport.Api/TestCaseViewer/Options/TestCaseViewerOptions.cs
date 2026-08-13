namespace ImpactSupport.Api.TestCaseViewer.Options
{
    public class TestCaseViewerOptions
    {
        public string[] DefaultSheetNames { get; set; } =
        ["Testcase_2026", "Testcase_2025"];

        public int AutoRefreshMinutes { get; set; } = 5;

        public Dictionary<string, string> KnownFileIds { get; set; } = [];
    }
}
