namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class PlaywrightReadinessResponse
{
    public bool PlaywrightProjectFound { get; set; }
    public bool TaggedSpecsFound { get; set; }
    public bool ModuleTagsFound { get; set; }
    public bool TypeTagsFound { get; set; }
    public bool RoleTagsFound { get; set; }
    public bool ClientTagsFound { get; set; }
    public bool NodeAvailable { get; set; }
    public bool NpmAvailable { get; set; }
    public bool PlaywrightAvailable { get; set; }
    public bool BrowsersAvailable { get; set; }
    public bool ManualMasterDataAvailable { get; set; }
    public bool RoleGateAvailable { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;
    public string PlaywrightTestsRef { get; set; } = string.Empty;
    public IReadOnlyList<string> BlockingIssues { get; set; } = [];
}

public sealed class TestRunConfigRequest
{
    public string TestingName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<string> Modules { get; set; } = [];
    public IReadOnlyList<string> TestingTypes { get; set; } = [];
    public string RoleBased { get; set; } = "ALL";
    public string RoleBasedClient { get; set; } = "ALL";
    public string Ui { get; set; } = "off";
    public string Client { get; set; } = "ALL";
    public string ContentType { get; set; } = "books";
    public string Domain { get; set; } = "UAT";
    public string RoleWorkflow { get; set; } = "author_editor_collator";
    public string TestingUrl { get; set; } = "author";
    public string MantisTicket { get; set; } = string.Empty;
    public string RefStyle { get; set; } = "number";
}

public sealed class TestRunConfigResponse
{
    public int Id { get; set; }
    public string TestingName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<string> Modules { get; set; } = [];
    public IReadOnlyList<string> TestingTypes { get; set; } = [];
    public string RoleBased { get; set; } = "ALL";
    public string RoleBasedClient { get; set; } = "ALL";
    public string Ui { get; set; } = "off";
    public bool IsFullRun { get; set; }
    public WorkflowContextResponse WorkflowContext { get; set; } = new();
}

public sealed class WorkflowContextResponse
{
    public string Client { get; set; } = "ALL";
    public string ContentType { get; set; } = "books";
    public string Domain { get; set; } = "UAT";
    public string RoleWorkflow { get; set; } = "author_editor_collator";
    public string TestingUrl { get; set; } = "author";
    public string MantisTicket { get; set; } = string.Empty;
    public string RefStyle { get; set; } = "number";
}

public sealed class TestRunExecutionResponse
{
    public int Id { get; set; }
    public int ConfigId { get; set; }
    public string TestingName { get; set; } = string.Empty;
    public string TriggeredBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PlaywrightCommand { get; set; } = string.Empty;
    public string PlaywrightTestsRef { get; set; } = string.Empty;
    public string ReportPath { get; set; } = string.Empty;
    public string RunKind { get; set; } = "STANDARD";
    public string ModuleName { get; set; } = string.Empty;
    public string TestCaseId { get; set; } = string.Empty;
    public string MantisTicket { get; set; } = string.Empty;
    public string FixSignal { get; set; } = string.Empty;
    public int? ExitCode { get; set; }
    public string FailureSummary { get; set; } = string.Empty;
    public DateTimeOffset TriggeredAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
}

public sealed class RunMetadataResponse
{
    public IReadOnlyList<string> Modules { get; set; } = [];
    public IReadOnlyList<string> TestingTypes { get; set; } = [];
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<string> Clients { get; set; } = [];
    public IReadOnlyList<string> ContentTypes { get; set; } = [];
    public IReadOnlyList<string> Domains { get; set; } = [];
    public IReadOnlyList<string> RoleWorkflows { get; set; } = [];
    public IReadOnlyList<string> TestingUrls { get; set; } = [];
    public IReadOnlyList<string> RefStyles { get; set; } = [];
}

public sealed class RecentRunsResponse
{
    public IReadOnlyList<TestRunExecutionResponse> Runs { get; set; } = [];
}

public sealed class RunProgressResponse
{
    public int ConfigId { get; set; }
    public string LastModuleName { get; set; } = string.Empty;
    public string NextModuleName { get; set; } = string.Empty;
    public int? LastExecutionId { get; set; }
}

public sealed class VerifyFixRequest
{
    public string TestCaseId { get; set; } = string.Empty;
    public string MantisTicket { get; set; } = string.Empty;
}
