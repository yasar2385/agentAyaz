using System;
using System.Threading.Tasks;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class SqliteUserAuthServiceTests
{
    [Fact]
    public async Task ValidateUserAsync_ReturnsUser_ForActiveUserWithValidPassword()
    {
        await using var fixture = await CreateFixtureAsync(isActive: true);

        var user = await fixture.Service.ValidateUserAsync("yasar@example.com", "secret");

        Assert.NotNull(user);
        Assert.Equal("yasar@example.com", user!.Username);
        Assert.Equal("Yasar", user.DisplayName);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsNull_ForInactiveUser()
    {
        await using var fixture = await CreateFixtureAsync(isActive: false);

        var user = await fixture.Service.ValidateUserAsync("yasar@example.com", "secret");

        Assert.Null(user);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsNull_ForUnknownUser()
    {
        await using var fixture = await CreateFixtureAsync(isActive: true);

        var user = await fixture.Service.ValidateUserAsync("missing@example.com", "secret");

        Assert.Null(user);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsNull_ForBadPassword()
    {
        await using var fixture = await CreateFixtureAsync(isActive: true);

        var user = await fixture.Service.ValidateUserAsync("yasar@example.com", "wrong");

        Assert.Null(user);
    }

    private static async Task<AuthFixture> CreateFixtureAsync(bool isActive)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new SupportDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        dbContext.TestCaseViewerUsers.Add(new TestCaseViewerUser
        {
            MongoId = "mongo-1",
            ExternalId = "external-1",
            Username = "yasar@example.com",
            Email = "yasar@example.com",
            DisplayName = "Yasar",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("secret"),
            RoleJson = "[\"role-1\"]",
            IsActive = isActive
        });
        await dbContext.SaveChangesAsync();

        var service = new SqliteUserAuthService(dbContext, NullLogger<SqliteUserAuthService>.Instance);
        return new AuthFixture(connection, dbContext, service);
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly SupportDbContext _dbContext;

        public AuthFixture(SqliteConnection connection, SupportDbContext dbContext, SqliteUserAuthService service)
        {
            _connection = connection;
            _dbContext = dbContext;
            Service = service;
        }

        public SqliteUserAuthService Service { get; }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
