using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace ImpactSupport.Api.TestCaseViewer.Services;

public sealed class SelectedMongoUserImportService : IHostedService
{
    private static readonly string[] UsernamePrefixes = ["yasar", "siva", "durai", "srinivasa", "divya"];

    private readonly IServiceProvider _serviceProvider;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SelectedMongoUserImportService> _logger;

    public SelectedMongoUserImportService(
        IServiceProvider serviceProvider,
        IWebHostEnvironment environment,
        ILogger<SelectedMongoUserImportService> logger)
    {
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var bsonPath = ResolveBsonPath();
        if (!File.Exists(bsonPath))
        {
            _logger.LogWarning("Selected Mongo user import skipped. BSON file not found at {BsonPath}", bsonPath);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDbContext>();

        var selectedDocuments = new Dictionary<string, BsonDocument>(StringComparer.OrdinalIgnoreCase);
        foreach (var document in ReadDocuments(bsonPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var username = GetString(document, "username");
            if (!IsAllowedUsername(username))
            {
                continue;
            }

            var mongoId = GetString(document, "_id");
            if (string.IsNullOrWhiteSpace(mongoId))
            {
                continue;
            }

            selectedDocuments[username] = document;
        }

        var imported = 0;
        foreach (var document in selectedDocuments.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var username = GetString(document, "username");
            var mongoId = GetString(document, "_id");
            var user = await dbContext.TestCaseViewerUsers
                .FirstOrDefaultAsync(x => x.MongoId == mongoId || x.Username == username, cancellationToken);

            if (user is null)
            {
                user = new TestCaseViewerUser { MongoId = mongoId };
                dbContext.TestCaseViewerUsers.Add(user);
            }

            user.MongoId = mongoId;
            user.ExternalId = GetString(document, "id");
            user.Username = username;
            user.Email = GetString(document, "email");
            user.DisplayName = GetString(document, "displayName");
            user.PasswordHash = GetString(document, "_hashed_password");
            user.RoleJson = document.TryGetValue("_role", out var roleValue) && !roleValue.IsBsonNull
                ? roleValue.ToJson()
                : string.Empty;
            user.IsActive = GetBoolean(document, "active", defaultValue: true);
            user.CreatedAtUtc = GetDateTime(document, "_created_at");
            user.UpdatedAtUtc = GetDateTime(document, "_updated_at");
            imported++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Selected Mongo user import completed. Upserted {ImportedCount} users.", imported);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private string ResolveBsonPath()
    {
        var seedPath = Path.Combine(_environment.ContentRootPath, "TestCaseViewer", "Data", "MongoSeed", "xmleditor", "User.bson");
        if (File.Exists(seedPath))
        {
            return seedPath;
        }

        var repositoryRoot = Directory.GetParent(_environment.ContentRootPath)?.Parent?.FullName;
        return repositoryRoot is null
            ? seedPath
            : Path.Combine(repositoryRoot, "mongo-backup-20260814-081014", "xmleditor", "User.bson");
    }

    private static bool IsAllowedUsername(string username)
    {
        return UsernamePrefixes.Any(prefix => username.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<BsonDocument> ReadDocuments(string bsonPath)
    {
        using var stream = File.OpenRead(bsonPath);
        while (stream.Position < stream.Length)
        {
            var reader = new BsonBinaryReader(stream);
            yield return BsonSerializer.Deserialize<BsonDocument>(reader);
        }
    }

    private static string GetString(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return string.Empty;
        }

        return value.ToString() ?? string.Empty;
    }

    private static bool GetBoolean(BsonDocument document, string fieldName, bool defaultValue)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        return value.BsonType switch
        {
            BsonType.Boolean => value.AsBoolean,
            BsonType.Int32 => value.AsInt32 != 0,
            BsonType.Int64 => value.AsInt64 != 0,
            BsonType.Double => Math.Abs(value.AsDouble) > double.Epsilon,
            BsonType.String => bool.TryParse(value.AsString, out var parsed) ? parsed : value.AsString != "0",
            _ => defaultValue
        };
    }

    private static DateTime? GetDateTime(BsonDocument document, string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return null;
        }

        return value.BsonType switch
        {
            BsonType.DateTime => value.ToUniversalTime(),
            BsonType.Int64 => DateTimeOffset.FromUnixTimeMilliseconds(value.AsInt64).UtcDateTime,
            BsonType.Double => DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(value.AsDouble)).UtcDateTime,
            _ => null
        };
    }
}
