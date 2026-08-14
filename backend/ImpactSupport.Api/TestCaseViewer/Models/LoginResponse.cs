namespace ImpactSupport.Api.TestCaseViewer.Models;

public sealed class LoginResponse
{
    public bool Authenticated { get; set; }
    public AuthUser? User { get; set; }
}
