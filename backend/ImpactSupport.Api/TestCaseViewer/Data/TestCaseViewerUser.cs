namespace ImpactSupport.Api.TestCaseViewer.Data;

public sealed class TestCaseViewerUser
{
    public long Id { get; set; }
    public string MongoId { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string RoleJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
