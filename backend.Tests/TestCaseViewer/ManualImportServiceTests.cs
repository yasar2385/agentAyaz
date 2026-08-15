using System.IO;
using System.Linq;
using System.Text;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class ManualImportServiceTests
{
    [Fact]
    public async Task UploadMasterAsync_GroupsMultipleSheetsAndDetectsExistingSheet()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "Login");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);

        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module", "QA Status", "Dev. Status"],
            ["Login", "TC_LOGIN_001", "Auth", "Pass", "Fixed"],
            ["Billing", "TC_BILL_001", "Billing", "Fail", "WIP"])), null);

        Assert.Equal(2, batch.SheetsDetected);
        Assert.Equal(1, batch.ExistingSheets);
        Assert.Equal(1, batch.NewSheets);
        Assert.Contains(batch.Sheets, sheet => sheet.SheetName == "Login" && sheet.ConflictStatus == "EXISTS");
        Assert.Contains(batch.Sheets, sheet => sheet.SheetName == "Billing" && sheet.ConflictStatus == "NEW");
    }

    [Fact]
    public async Task CommitAsync_BlocksExistingSheetWithoutAction()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "Login");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module"],
            ["Login", "TC_LOGIN_001", "Auth"])), null);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CommitAsync(batch.BatchId));

        Assert.Contains("Choose overwrite or skip", error.Message);
    }

    [Fact]
    public async Task CommitAsync_SkipsExistingSheetWhenRequested()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "Login", "Old");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module"],
            ["Login", "TC_LOGIN_001", "New"])), null);
        var sheet = Assert.Single(batch.Sheets);
        await service.SaveSheetActionsAsync(batch.BatchId, new() { Actions = [new() { SheetId = sheet.Id, Action = "SKIP" }] });

        await service.CommitAsync(batch.BatchId);

        var cached = await db.QaDashboardSheetCaches.SingleAsync(item => item.SheetName == "Login");
        Assert.Equal("Old", cached.Module);
    }

    [Fact]
    public async Task CommitAsync_OverwritesExistingSheetWhenRequested()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "Login", "Old");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module", "QA Status"],
            ["Login", "TC_LOGIN_001", "New", "Pass"])), null);
        var sheet = Assert.Single(batch.Sheets);
        await service.SaveSheetActionsAsync(batch.BatchId, new() { Actions = [new() { SheetId = sheet.Id, Action = "OVERWRITE" }] });

        await service.CommitAsync(batch.BatchId);

        var cached = await db.QaDashboardSheetCaches.SingleAsync(item => item.SheetName == "Login");
        Assert.Equal("New", cached.Module);
        Assert.Equal(1, cached.TotalTestCases);
        Assert.Equal(1, cached.PassCount);
    }

    [Fact]
    public async Task UploadMasterAsync_HandlesQuotedMultilineTsvAndUsesFileNameWhenSheetNameIsDash()
    {
        await using var db = CreateDbContext();
        var service = new ManualImportService(db);
        var content = string.Join('\n',
            "Sheet Name\tTest Case ID\tModule/ Sub Module\tPreconditions\tType of testing\tTest Case Description\tQA Status\tDev. Status\tQA Remarks\tDev. Remarks\tQA Remarks",
            "-\tTC_CS_001\tContact Support\tAll user\tTomcat_Regression\t\"Line one\nLine two\"\t\t\tQA1\tDev1\tIgnored fifth");

        var batch = await service.UploadMasterAsync(File("Sample_Regression testing_Contact Support.tsv", content), null);

        Assert.Equal(1, batch.SheetsDetected);
        var sheet = Assert.Single(batch.Sheets);
        Assert.Equal("Sample_Regression testing_Contact Support", sheet.SheetName);
        Assert.Equal("Contact Support", sheet.ModuleName);
        Assert.Equal(0, batch.RowsError);
    }

    [Fact]
    public async Task UploadMasterAsync_ReportsUnknownPreconditionsInsteadOfWildcard()
    {
        await using var db = CreateDbContext();
        var service = new ManualImportService(db);

        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module", "Preconditions"],
            ["Contact Support", "TC_CS_001", "Contact Support", "Unexpected access group"])), null);

        Assert.Equal(1, batch.RowsError);
        var error = await db.QaImportBatchErrors.SingleAsync();
        Assert.Contains("Unrecognized Preconditions", error.ErrorMessage);
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

    private static void SeedExistingSheet(SupportDbContext db, string sheetName, string module = "Auth")
    {
        var file = new QaDashboardFileCache
        {
            FileId = "manual-master",
            FileName = "Manual Master",
            ReportType = "master"
        };
        file.Sheets.Add(new QaDashboardSheetCache
        {
            FileId = file.FileId,
            SheetName = sheetName,
            Module = module,
            PurposeOfTesting = module,
            RowsJson = "[]"
        });
        db.QaDashboardFileCaches.Add(file);
    }

    private static IFormFile File(string fileName, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    private static string Tsv(params string[][] rows)
    {
        return string.Join('\n', rows.Select(row => string.Join('\t', row)));
    }
}
