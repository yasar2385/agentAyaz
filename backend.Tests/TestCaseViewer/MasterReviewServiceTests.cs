using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class MasterReviewServiceTests
{
    [Fact]
    public async Task GetModulesAsync_ReturnsCommittedCounts()
    {
        await using var db = CreateDbContext();
        db.MasterModules.Add(new MasterModule { Id = 50, Name = "Contact Support" });
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_001", MasterModules = 50 });
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_002", MasterModules = 50 });
        await db.SaveChangesAsync();

        var modules = await new MasterReviewService(db).GetModulesAsync();

        Assert.Contains(modules, module => module.ModuleId == 50 && module.TestCaseCount == 2);
    }

    [Fact]
    public async Task UpdateAsync_ValidEditWritesAuditHistory()
    {
        await using var db = CreateDbContext();
        var master = new MasterTemplate { MasterTestId = "TC_REVIEW_003", MasterModules = 1, MasterQaStatus = 1, Details = new MasterTestDetails() };
        db.MasterTemplates.Add(master);
        await db.SaveChangesAsync();
        var service = new MasterReviewService(db);
        var detail = await service.GetDetailAsync("TC_REVIEW_003");

        var saved = await service.UpdateAsync("TC_REVIEW_003", new MasterTemplateUpdateRequest
        {
            LastKnownUpdatedAt = detail!.MasterUpdatedAt,
            QaStatusId = 2,
            MasterDescription = "Corrected description",
            Remarks = [new() { RoundNumber = 1, QaRemark = "QA note", DevRemark = "Dev note" }]
        }, QaUser());

        Assert.Equal(2, saved!.QaStatusId);
        Assert.Equal("QA User", saved.MasterUpdatedBy);
        Assert.Contains(saved.EditHistory, item => item.FieldName == "MasterQaStatus");
        Assert.Contains(saved.EditHistory, item => item.FieldName == "MasterDescription");
        Assert.Contains(saved.Remarks, item => item.RoundNumber == 1 && item.QaRemark == "QA note");
    }

    [Fact]
    public async Task UpdateAsync_InvalidLookupRejects()
    {
        await using var db = CreateDbContext();
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_004" });
        await db.SaveChangesAsync();
        var service = new MasterReviewService(db);
        var detail = await service.GetDetailAsync("TC_REVIEW_004");

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateAsync("TC_REVIEW_004", new MasterTemplateUpdateRequest
        {
            LastKnownUpdatedAt = detail!.MasterUpdatedAt,
            QaStatusId = 999
        }, QaUser()));
    }

    [Fact]
    public async Task UpdateAsync_StaleTimestampRejects()
    {
        await using var db = CreateDbContext();
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_005", MasterUpdatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => new MasterReviewService(db).UpdateAsync("TC_REVIEW_005", new MasterTemplateUpdateRequest
        {
            LastKnownUpdatedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            MasterTestNo = "new"
        }, QaUser()));
    }

    [Fact]
    public async Task UpdateAsync_NonQaDevManagerRejects()
    {
        await using var db = CreateDbContext();
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_006" });
        await db.SaveChangesAsync();
        var detail = await new MasterReviewService(db).GetDetailAsync("TC_REVIEW_006");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new MasterReviewService(db).UpdateAsync("TC_REVIEW_006", new MasterTemplateUpdateRequest
        {
            LastKnownUpdatedAt = detail!.MasterUpdatedAt,
            MasterTestNo = "new"
        }, new AuthUser { Username = "viewer", Role = "Viewer" }));
    }

    private static SupportDbContext CreateDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new SupportDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static AuthUser QaUser() => new()
    {
        Username = "qa.user",
        DisplayName = "QA User",
        Role = "QA"
    };
}
