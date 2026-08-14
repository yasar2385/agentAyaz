using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class MongoUserAuthService : IUserAuthService
{
    private readonly MongoAuthOptions _options;
    private readonly ILogger<MongoUserAuthService> _logger;

    public MongoUserAuthService(IOptions<MongoAuthOptions> options, ILogger<MongoUserAuthService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthUser?> ValidateUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString) || string.IsNullOrWhiteSpace(_options.DatabaseName))
        {
            throw new InvalidOperationException("MongoAuth connection settings are not configured.");
        }

        var client = new MongoClient(_options.ConnectionString);
        var collection = client
            .GetDatabase(_options.DatabaseName)
            .GetCollection<BsonDocument>(_options.UsersCollectionName);

        var filter = Builders<BsonDocument>.Filter.Eq(_options.UsernameField, username);
        var user = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (user is null || IsInactive(user))
        {
            return null;
        }

        var storedPassword = GetString(user, _options.PasswordHashField);
        if (string.IsNullOrWhiteSpace(storedPassword))
        {
            storedPassword = GetString(user, _options.PasswordField);
        }

        if (!VerifyPassword(password, storedPassword))
        {
            return null;
        }

        return new AuthUser
        {
            Id = user.TryGetValue("_id", out var id) ? id.ToString() ?? string.Empty : string.Empty,
            Username = GetString(user, _options.UsernameField),
            DisplayName = GetString(user, _options.DisplayNameField),
            Role = GetString(user, _options.RoleField),
            Email = GetString(user, _options.EmailField)
        };
    }

    private bool IsInactive(BsonDocument user)
    {
        if (!user.TryGetValue(_options.IsActiveField, out var value))
        {
            return false;
        }

        return value.IsBoolean && !value.AsBoolean;
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

    private static string GetString(BsonDocument document, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName) || !document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }
}
