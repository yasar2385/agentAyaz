namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class MasterModuleSummaryResponse
{
    public int ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int TestCaseCount { get; set; }
}

public sealed class MasterTemplateListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public IReadOnlyList<MasterTemplateSummaryResponse> Items { get; set; } = [];
}

public sealed class MasterTemplateListRequest
{
    public int? ModuleId { get; set; }
    public int? ClientId { get; set; }
    public int? RoleId { get; set; }
    public int? Round { get; set; }
    public string Search { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public sealed class MasterTemplateSummaryResponse
{
    public int MasterId { get; set; }
    public string MasterTestId { get; set; } = string.Empty;
    public string MasterTestNo { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public string QaStatus { get; set; } = string.Empty;
    public string DevStatus { get; set; } = string.Empty;
    public DateTimeOffset MasterUpdatedAt { get; set; }
    public string MasterUpdatedBy { get; set; } = string.Empty;
}

public sealed class MasterLookupsResponse
{
    public IReadOnlyList<LookupItemResponse> Modules { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> PreconditionRoles { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> TestingTypes { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> IssueTypes { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> QaStatuses { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> DevStatuses { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> Clients { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> ContentTypes { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> DtdTypes { get; set; } = [];
    public IReadOnlyList<LookupItemResponse> RoleWorkflows { get; set; } = [];
}

public sealed class LookupItemResponse
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
}

public sealed class MasterTemplateDetailResponse
{
    public int MasterId { get; set; }
    public string MasterTestId { get; set; } = string.Empty;
    public string MasterOriginalRawId { get; set; } = string.Empty;
    public string MasterTestNo { get; set; } = string.Empty;
    public string MasterSourceSheet { get; set; } = string.Empty;
    public int MasterSourceRow { get; set; }
    public int? ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public int? PreconditionRoleId { get; set; }
    public int? MasterTypeId { get; set; }
    public int? DtdTypeId { get; set; }
    public int? RoleWorkflowId { get; set; }
    public bool MasterIsCollaborative { get; set; }
    public bool MasterIsSharedRole { get; set; }
    public string MasterPreparedBy { get; set; } = string.Empty;
    public string MasterPreparedDate { get; set; } = string.Empty;
    public string MasterTestData { get; set; } = string.Empty;
    public string MasterExpectedResult { get; set; } = string.Empty;
    public string MasterActualResult { get; set; } = string.Empty;
    public int? IssueTypeId { get; set; }
    public int? QaStatusId { get; set; }
    public int? DevStatusId { get; set; }
    public string MasterDescription { get; set; } = string.Empty;
    public string MasterTestSteps { get; set; } = string.Empty;
    public IReadOnlyList<int> TestingTypeIds { get; set; } = [];
    public IReadOnlyList<int> ClientIds { get; set; } = [];
    public IReadOnlyList<MasterRemarkResponse> Remarks { get; set; } = [];
    public IReadOnlyList<MasterEditHistoryResponse> EditHistory { get; set; } = [];
    public DateTimeOffset MasterUpdatedAt { get; set; }
    public string MasterUpdatedBy { get; set; } = string.Empty;
}

public sealed class MasterRemarkResponse
{
    public int RoundNumber { get; set; }
    public string QaRemark { get; set; } = string.Empty;
    public string DevRemark { get; set; } = string.Empty;
}

public sealed class MasterEditHistoryResponse
{
    public int Id { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string EditedBy { get; set; } = string.Empty;
    public DateTimeOffset EditedAt { get; set; }
}

public class MasterTemplateUpdateRequest
{
    public DateTimeOffset LastKnownUpdatedAt { get; set; }
    public string? MasterTestNo { get; set; }
    public int? ModuleId { get; set; }
    public int? PreconditionRoleId { get; set; }
    public int? MasterTypeId { get; set; }
    public int? DtdTypeId { get; set; }
    public int? RoleWorkflowId { get; set; }
    public bool? MasterIsCollaborative { get; set; }
    public bool? MasterIsSharedRole { get; set; }
    public string? MasterPreparedBy { get; set; }
    public string? MasterPreparedDate { get; set; }
    public string? MasterTestData { get; set; }
    public string? MasterExpectedResult { get; set; }
    public string? MasterActualResult { get; set; }
    public int? IssueTypeId { get; set; }
    public int? QaStatusId { get; set; }
    public int? DevStatusId { get; set; }
    public string? MasterDescription { get; set; }
    public string? MasterTestSteps { get; set; }
    public IReadOnlyList<int>? TestingTypeIds { get; set; }
    public IReadOnlyList<int>? ClientIds { get; set; }
    public IReadOnlyList<MasterRemarkResponse>? Remarks { get; set; }
}

public sealed class MasterTemplateCreateRequest : MasterTemplateUpdateRequest
{
    public string MasterTestId { get; set; } = string.Empty;
}
