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
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_DELETED", MasterModules = 50, MasterIsActive = false });
        await db.SaveChangesAsync();

        var modules = await new MasterReviewService(db).GetModulesAsync();

        Assert.Contains(modules, module => module.ModuleId == 50 && module.TestCaseCount == 2);
    }

    [Fact]
    public async Task CreateAsync_ValidRequestCreatesDetailAndAuditHistory()
    {
        await using var db = CreateDbContext();
        db.MasterModules.Add(new MasterModule { Id = 51, Name = "Authoring" });
        await db.SaveChangesAsync();

        var saved = await new MasterReviewService(db).CreateAsync(new MasterTemplateCreateRequest
        {
            MasterTestId = "TC_REVIEW_CREATE",
            MasterTestNo = "1",
            ModuleId = 51,
            MasterDescription = "Manual description",
            MasterTestSteps = "Manual steps",
            TestingTypeIds = [1],
            Remarks = [new() { RoundNumber = 1, QaRemark = "QA", DevRemark = "Dev" }]
        }, QaUser());

        Assert.Equal("TC_REVIEW_CREATE", saved.MasterTestId);
        Assert.Equal(51, saved.ModuleId);
        Assert.Equal("QA User", saved.MasterUpdatedBy);
        Assert.Contains(saved.EditHistory, item => item.FieldName == "Create");
        Assert.Contains(saved.Remarks, item => item.RoundNumber == 1 && item.QaRemark == "QA");
    }

    [Fact]
    public async Task CreateAsync_DuplicateMasterTestIdRejects()
    {
        await using var db = CreateDbContext();
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_DUP" });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() => new MasterReviewService(db).CreateAsync(new MasterTemplateCreateRequest
        {
            MasterTestId = "TC_REVIEW_DUP"
        }, QaUser()));
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

    [Fact]
    public async Task DeleteAsync_SoftDeletesAndListExcludesButKeepsResultHistory()
    {
        await using var db = CreateDbContext();
        db.MasterModules.Add(new MasterModule { Id = 52, Name = "Review" });
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_REVIEW_DELETE", MasterModules = 52 });
        db.TestingMetaResults.Add(new TestingMetaResult
        {
            Name = "Manual result",
            DataResults = [new TestingDataResult { MasterTestId = "TC_REVIEW_DELETE", MasterQaStatus = 1 }]
        });
        await db.SaveChangesAsync();

        var service = new MasterReviewService(db);
        var deleted = await service.DeleteAsync("TC_REVIEW_DELETE", QaUser());
        var list = await service.GetListAsync(new MasterTemplateListRequest { ModuleId = 52, Page = 1, PageSize = 25 });
        var master = await db.MasterTemplates.IgnoreQueryFilters().SingleAsync(item => item.MasterTestId == "TC_REVIEW_DELETE");

        Assert.True(deleted);
        Assert.False(master.MasterIsActive);
        Assert.Equal("QA User", master.MasterDeletedBy);
        Assert.NotNull(master.MasterDeletedAt);
        Assert.Empty(list.Items);
        Assert.Equal(1, await db.TestingDataResults.CountAsync(item => item.MasterTestId == "TC_REVIEW_DELETE"));
        Assert.True(await db.MasterTemplateEditHistory.AnyAsync(item => item.MasterId == master.MasterId && item.FieldName == "Delete"));
    }

    [Fact]
    public async Task GetListAsync_AppliesCombinedFiltersAndExcludesInactiveRows()
    {
        await using var db = CreateDbContext();
        db.MasterModules.Add(new MasterModule { Id = 60, Name = "Landing Page" });
        db.MasterModules.Add(new MasterModule { Id = 61, Name = "Contact Support" });
        db.MasterTemplates.Add(new MasterTemplate
        {
            MasterTestId = "TC_FILTER_001",
            MasterModules = 60,
            MasterPreconditionRole = 4,
            Details = new MasterTestDetails { MasterDescription = "Searchable landing description" },
            Clients = [new MasterTemplateClient { ClientId = 5 }, new MasterTemplateClient { ClientId = 1 }],
            Remarks = [new MasterTemplateRemark { RoundNumber = 2, QaRemark = "Round two note", DevRemark = string.Empty }]
        });
        db.MasterTemplates.Add(new MasterTemplate
        {
            MasterTestId = "TC_FILTER_002",
            MasterModules = 60,
            MasterPreconditionRole = 1,
            Details = new MasterTestDetails { MasterDescription = "Searchable landing description" },
            Clients = [new MasterTemplateClient { ClientId = 5 }],
            Remarks = [new MasterTemplateRemark { RoundNumber = 1, QaRemark = "Round one note", DevRemark = string.Empty }]
        });
        db.MasterTemplates.Add(new MasterTemplate
        {
            MasterTestId = "TC_FILTER_DELETED",
            MasterModules = 60,
            MasterPreconditionRole = 4,
            MasterIsActive = false,
            Details = new MasterTestDetails { MasterDescription = "Searchable landing description" },
            Clients = [new MasterTemplateClient { ClientId = 5 }],
            Remarks = [new MasterTemplateRemark { RoundNumber = 2, QaRemark = "Round two note", DevRemark = string.Empty }]
        });
        await db.SaveChangesAsync();

        var list = await new MasterReviewService(db).GetListAsync(new MasterTemplateListRequest
        {
            ModuleId = 60,
            ClientId = 5,
            RoleId = 4,
            Round = 2,
            Search = "landing",
            Page = 1,
            PageSize = 25
        });

        var item = Assert.Single(list.Items);
        Assert.Equal("TC_FILTER_001", item.MasterTestId);
        Assert.Equal(1, list.TotalCount);
    }

    [Fact]
    public async Task GetListAsync_SearchMatchesTestIdAndDescription()
    {
        await using var db = CreateDbContext();
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_SEARCH_ID", Details = new MasterTestDetails { MasterDescription = "Plain text" } });
        db.MasterTemplates.Add(new MasterTemplate { MasterTestId = "TC_OTHER", Details = new MasterTestDetails { MasterDescription = "Special description text" } });
        await db.SaveChangesAsync();
        var service = new MasterReviewService(db);

        var byId = await service.GetListAsync(new MasterTemplateListRequest { Search = "search_id" });
        var byDescription = await service.GetListAsync(new MasterTemplateListRequest { Search = "special description" });

        Assert.Contains(byId.Items, item => item.MasterTestId == "TC_SEARCH_ID");
        Assert.DoesNotContain(byId.Items, item => item.MasterTestId == "TC_OTHER");
        Assert.Contains(byDescription.Items, item => item.MasterTestId == "TC_OTHER");
        Assert.DoesNotContain(byDescription.Items, item => item.MasterTestId == "TC_SEARCH_ID");
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
