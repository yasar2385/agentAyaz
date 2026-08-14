using ImpactSupport.Api.TestCaseViewer.Models;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public interface IUserAuthService
{
    Task<AuthUser?> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default);
}
