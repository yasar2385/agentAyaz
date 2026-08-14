using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using Microsoft.EntityFrameworkCore;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class SqliteUserAuthService : IUserAuthService
{
    private readonly SupportDbContext _dbContext;
    private readonly ILogger<SqliteUserAuthService> _logger;

    public SqliteUserAuthService(SupportDbContext dbContext, ILogger<SqliteUserAuthService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AuthUser?> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Trim();
        var user = await _dbContext.TestCaseViewerUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Username == normalizedUsername, cancellationToken);

        if (user is null || !user.IsActive || !VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return new AuthUser
        {
            Id = string.IsNullOrWhiteSpace(user.ExternalId) ? user.MongoId : user.ExternalId,
            Username = user.Username,
            DisplayName = user.DisplayName,
            Role = user.RoleJson,
            Email = user.Email
        };
    }

    private bool VerifyPassword(string password, string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            return false;
        }

        if (storedPassword.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "BCrypt password verification failed.");
                return false;
            }
        }

        return string.Equals(password, storedPassword, StringComparison.Ordinal);
    }
}
