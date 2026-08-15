using System.IO;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Options;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class PlaywrightReadinessServiceTests
{
    [Fact]
    public async Task GetReadinessAsync_BlocksMissingSpecRepo()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db, new PlaywrightOptions { WorkingDirectory = Path.Combine(Path.GetTempPath(), "missing-playwright-repo") });

        var readiness = await service.GetReadinessAsync();

        Assert.False(readiness.PlaywrightProjectFound);
        Assert.Contains(readiness.BlockingIssues, issue => issue.Contains("working directory"));
    }

    [Fact]
    public async Task GetReadinessAsync_DetectsTaggedSpecsAndMasterData()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"pw-{Guid.NewGuid():N}")).FullName;
        await File.WriteAllTextAsync(Path.Combine(dir, "playwright.config.js"), "module.exports = {}");
        Directory.CreateDirectory(Path.Combine(dir, "tests"));
        await File.WriteAllTextAsync(Path.Combine(dir, "tests", "sample.spec.js"), "test('x @module=CKEditor @type=Regression @role=Author @client=LWW', async () => {})");
        await using var db = CreateDbContext();
        var file = new QaDashboardFileCache { FileId = "manual-master", FileName = "Manual", ReportType = "master" };
        file.Sheets.Add(new QaDashboardSheetCache { FileId = file.FileId, SheetName = "CKEditor", Module = "CKEditor", TotalTestCases = 1 });
        db.QaDashboardFileCaches.Add(file);
        await db.SaveChangesAsync();
        var service = CreateService(db, new PlaywrightOptions { WorkingDirectory = dir, Command = "node" });

        var readiness = await service.GetReadinessAsync();

        Assert.True(readiness.PlaywrightProjectFound);
        Assert.True(readiness.ModuleTagsFound);
        Assert.True(readiness.TypeTagsFound);
        Assert.True(readiness.RoleTagsFound);
        Assert.True(readiness.ClientTagsFound);
        Assert.True(readiness.ManualMasterDataAvailable);
    }

    private static PlaywrightRunService CreateService(SupportDbContext db, PlaywrightOptions options)
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<PlaywrightCommandBuilder>();
        services.AddLogging();
        services.AddSingleton<IServiceScopeFactory>(new EmptyScopeFactory());
        return new PlaywrightRunService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            db,
            Options.Create(options),
            new PlaywrightCommandBuilder(Options.Create(options), NullLogger<PlaywrightCommandBuilder>.Instance),
            NullLogger<PlaywrightRunService>.Instance);
    }

    private static SupportDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SupportDbContext>().UseSqlite(connection).Options;
        var db = new SupportDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private sealed class EmptyScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new EmptyScope();
    }

    private sealed class EmptyScope : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
        public void Dispose() { }
    }
}
