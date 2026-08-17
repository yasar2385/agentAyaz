namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class MasterTemplate
{
    public int MasterId { get; set; }
    public string MasterTestId { get; set; } = string.Empty;
    public string? MasterOriginalRawId { get; set; }
    public string MasterTestNo { get; set; } = string.Empty;
    public string MasterSourceSheet { get; set; } = string.Empty;
    public int MasterSourceRow { get; set; }
    public int? MasterModules { get; set; }
    public int? MasterPreconditionRole { get; set; }
    public int? MasterClient { get; set; }
    public int? MasterSubClient { get; set; }
    public int? MasterType { get; set; }
    public int? MasterDtdType { get; set; }
    public int? MasterRoleWorkflow { get; set; }
    public bool MasterIsCollaborative { get; set; }
    public bool MasterIsSharedRole { get; set; }
    public bool MasterIsActive { get; set; } = true;
    public DateTimeOffset? MasterDeletedAt { get; set; }
    public string? MasterDeletedBy { get; set; }
    public string MasterPreparedBy { get; set; } = string.Empty;
    public string MasterPreparedDate { get; set; } = string.Empty;
    public string MasterTestData { get; set; } = string.Empty;
    public string MasterExpectedResult { get; set; } = string.Empty;
    public string MasterActualResult { get; set; } = string.Empty;
    public int? MasterIssueType { get; set; }
    public int? MasterQaStatus { get; set; }
    public int? MasterDevStatus { get; set; }
    public DateTimeOffset MasterCreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset MasterUpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? MasterUpdatedBy { get; set; }
    public MasterTestDetails? Details { get; set; }
    public List<MasterTemplateTestingType> TestingTypes { get; set; } = [];
    public List<MasterTemplateRemark> Remarks { get; set; } = [];
    public List<MasterTemplateClient> Clients { get; set; } = [];
    public List<MasterTemplateEditHistory> EditHistory { get; set; } = [];
}

public sealed class MasterTestDetails
{
    public int MasterId { get; set; }
    public string MasterDescription { get; set; } = string.Empty;
    public string MasterTestSteps { get; set; } = string.Empty;
    public MasterTemplate? MasterTemplate { get; set; }
}

public sealed class MasterModule { public int Id { get; set; } public string Name { get; set; } = string.Empty; }
public sealed class MasterPreconditionRole { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class MasterPreconditionRoleAlias { public string Alias { get; set; } = string.Empty; public int RoleId { get; set; } }
public sealed class MasterTestingType { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class MasterTestingTypeAlias { public string Alias { get; set; } = string.Empty; public int TestingTypeId { get; set; } }
public sealed class MasterIssueType { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class MasterQaStatus { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class MasterDevStatus { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class Client { public int Id { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; }
public sealed class ClientAlias { public string Alias { get; set; } = string.Empty; public int ClientId { get; set; } }
public sealed class ClientSubBrand { public int Id { get; set; } public int ClientId { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class RefStyle { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class RoleWorkflow { public int Id { get; set; } public string Value { get; set; } = string.Empty; public bool IsDefault { get; set; } }
public sealed class ContentType { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class DtdType { public int Id { get; set; } public string Value { get; set; } = string.Empty; }
public sealed class TestingUrl { public int Id { get; set; } public string Value { get; set; } = string.Empty; public string UrlType { get; set; } = "single"; }
public sealed class TypeClientDtdMap { public int Id { get; set; } public int TypeId { get; set; } public int ClientId { get; set; } public int? SubClientId { get; set; } public int DtdTypeId { get; set; } }

public sealed class MasterTemplateTestingType
{
    public int MasterId { get; set; }
    public int TestingTypeId { get; set; }
    public MasterTemplate? MasterTemplate { get; set; }
}

public sealed class MasterTemplateClient
{
    public int MasterId { get; set; }
    public int ClientId { get; set; }
    public MasterTemplate? MasterTemplate { get; set; }
}

public sealed class MasterTemplateRemark
{
    public int Id { get; set; }
    public int MasterId { get; set; }
    public int RoundNumber { get; set; }
    public string QaRemark { get; set; } = string.Empty;
    public string DevRemark { get; set; } = string.Empty;
    public MasterTemplate? MasterTemplate { get; set; }
}

public sealed class MasterTemplateEditHistory
{
    public int Id { get; set; }
    public int MasterId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public string EditedBy { get; set; } = string.Empty;
    public DateTimeOffset EditedAt { get; set; } = DateTimeOffset.UtcNow;
    public MasterTemplate? MasterTemplate { get; set; }
}

public sealed class TestingMetaResult
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? RunBy { get; set; }
    public DateTimeOffset RunAt { get; set; } = DateTimeOffset.UtcNow;
    public string RunThrough { get; set; } = "MANUAL";
    public List<TestingDataResult> DataResults { get; set; } = [];
    public List<TestingMetaResultModuleStat> ModuleStats { get; set; } = [];
}

public sealed class TestingMetaResultLink
{
    public int TestingMetaResultId { get; set; }
    public int RoleId { get; set; }
    public int TestingUrlId { get; set; }
}

public sealed class TestingMetaResultTestingType
{
    public int TestingMetaResultId { get; set; }
    public int TestingTypeId { get; set; }
}

public sealed class TestingMetaResultModuleStat
{
    public int TestingMetaResultId { get; set; }
    public int MasterModuleId { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public TestingMetaResult? TestingMetaResult { get; set; }
}

public sealed class TestingDataResult
{
    public int Id { get; set; }
    public int TestingMetaResultId { get; set; }
    public string MasterTestId { get; set; } = string.Empty;
    public int? MasterIssueType { get; set; }
    public int? MasterQaStatus { get; set; }
    public int? MasterDevStatus { get; set; }
    public TestingMetaResult? TestingMetaResult { get; set; }
}
