namespace ImpactSupport.Api.TestCaseViewer.Options
{
    public class TestCaseViewerOptions
    {
        public string[] DefaultSheetNames { get; set; } =
        ["Testcase_2026", "Testcase_2025"];

        public int AutoRefreshMinutes { get; set; } = 5;

        public Dictionary<string, string> KnownFileIds { get; set; } = [];

        public List<TestCaseViewerAccessRule> AccessRules { get; set; } = [];
    }

    public class TestCaseViewerAccessRule
    {
        public string[] Usernames { get; set; } = [];
        public string[] Roles { get; set; } = [];
        public string[] ReportTypes { get; set; } = [];
        public string[] FilePatterns { get; set; } = [];
        public string[] SheetPatterns { get; set; } = [];
        public string[] ModulePatterns { get; set; } = [];
    }
}
