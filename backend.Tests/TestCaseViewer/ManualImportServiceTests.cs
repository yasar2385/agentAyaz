using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ImpactSupport.Api.Support.Data;
using ImpactSupport.Api.TestCaseViewer.Data;
using ImpactSupport.Api.TestCaseViewer.Models;
using ImpactSupport.Api.TestCaseViewer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ImpactSupport.Api.Tests.TestCaseViewer;

public sealed class ManualImportServiceTests
{
    [Fact]
    public async Task UploadMasterAsync_UsesUploadFileNameAsSourceSheetAndDetectsExistingSheet()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "master");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);

        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module", "QA Status", "Dev. Status"],
            ["Login", "TC_LOGIN_001", "Auth", "Pass", "Fixed"],
            ["Billing", "TC_BILL_001", "Billing", "Fail", "WIP"])), null);

        Assert.Equal(1, batch.SheetsDetected);
        Assert.Equal(1, batch.ExistingSheets);
        Assert.Equal(0, batch.NewSheets);
        Assert.Contains(batch.Sheets, sheet => sheet.SheetName == "master" && sheet.ConflictStatus == "EXISTS");
    }

    [Fact]
    public async Task CommitAsync_BlocksExistingSheetWithoutAction()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "master");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module"],
            ["ignored", "TC_LOGIN_001", "Auth"])), null);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CommitAsync(batch.BatchId));

        Assert.Contains("Choose overwrite or skip", error.Message);
    }

    [Fact]
    public async Task CommitAsync_SkipsExistingSheetWhenRequested()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "master", "Old");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module"],
            ["ignored", "TC_LOGIN_001", "New"])), null);
        var sheet = Assert.Single(batch.Sheets);
        await service.SaveSheetActionsAsync(batch.BatchId, new() { Actions = [new() { SheetId = sheet.Id, Action = "SKIP" }] });

        await service.CommitAsync(batch.BatchId);

        var cached = await db.QaDashboardSheetCaches.SingleAsync(item => item.SheetName == "master");
        Assert.Equal("Old", cached.Module);
    }

    [Fact]
    public async Task CommitAsync_OverwritesExistingSheetWhenRequested()
    {
        await using var db = CreateDbContext();
        SeedExistingSheet(db, "master", "Old");
        await db.SaveChangesAsync();
        var service = new ManualImportService(db);
        var batch = await service.UploadMasterAsync(File("master.tsv", Tsv(
            ["Sheet Name", "Test Case ID", "Module/Sub Module", "QA Status"],
            ["ignored", "TC_LOGIN_001", "New", "Pass"])), null);
        var sheet = Assert.Single(batch.Sheets);
        await service.SaveSheetActionsAsync(batch.BatchId, new() { Actions = [new() { SheetId = sheet.Id, Action = "OVERWRITE" }] });

        await service.CommitAsync(batch.BatchId);

        var cached = await db.QaDashboardSheetCaches.SingleAsync(item => item.SheetName == "master");
        Assert.Equal("New", cached.Module);
        Assert.Equal(1, cached.TotalTestCases);
        Assert.Equal(1, cached.PassCount);
        var master = await db.MasterTemplates.Include(item => item.Details).Include(item => item.TestingTypes).SingleAsync();
        Assert.Equal("TC_LOGIN_001", master.MasterTestId);
        Assert.Equal("master", master.MasterSourceSheet);
        Assert.Equal("New", (await db.MasterModules.FindAsync(master.MasterModules))?.Name);
    }

    [Fact]
    public async Task UploadMasterAsync_HandlesQuotedMultilineTsvAndUsesFileNameWhenSheetNameIsDash()
    {
        await using var db = CreateDbContext();
        var service = new ManualImportService(db);
        var content = string.Join('\n',
            "Sheet Name\tTest Case ID\tModule/ Sub Module\tPreconditions\tType of testing\tTest Case Description\tQA Status\tDev. Status\tQA Remarks\tDev. Remarks\tQA Remarks",
            "-\tTC_CS_001\tContact Support\tAll user\tTomcat_Reg\t\"Line one\nLine two\"\t\t\tQA1\tDev1\tIgnored fifth");

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
        Assert.Empty(await db.QaImportBatchErrors.ToListAsync());
        var error = Assert.Single(batch.Errors);
        Assert.Contains("Unrecognized Preconditions", error.ErrorMessage);
    }

    [Fact]
    public async Task UploadMasterAsync_ReadsXlsxWorksheetsAndNormalizesCellLineBreaks()
    {
        await using var db = CreateDbContext();
        var service = new ManualImportService(db);

        var batch = await service.UploadMasterAsync(XlsxFile("master.xlsx"), null);

        Assert.Equal("XLSX workbook", batch.SourceType);
        Assert.Equal(2, batch.SheetsDetected);
        Assert.Contains(batch.Sheets, sheet => sheet.SheetName == "Contact Support");
        Assert.Contains(batch.Sheets, sheet => sheet.SheetName == "Billing");
        Assert.Equal(0, batch.RowsError);
        await service.CommitAsync(batch.BatchId);
        var contact = await db.MasterTemplates.Include(item => item.Details).SingleAsync(item => item.MasterTestId == "TC_XLSX_001");
        Assert.Equal("Contact Support", contact.MasterSourceSheet);
        Assert.Equal("Line one\nLine two", contact.Details?.MasterDescription);
    }

    [Fact]
    public async Task InspectAndParseMasterAsync_ExcludesHiddenSheetsUntilSelected()
    {
        await using var db = CreateDbContext();
        var service = new ManualImportService(db);

        var inspect = await service.InspectAsync(XlsxFile("master.xlsx", includeHiddenStates: true));

        Assert.Empty(await db.QaImportBatches.ToListAsync());
        Assert.Contains(inspect.Sheets, sheet => sheet.SheetName == "Contact Support" && sheet.Visibility == "visible");
        Assert.Contains(inspect.Sheets, sheet => sheet.SheetName == "Billing" && sheet.Visibility == "hidden");
        Assert.Contains(inspect.Sheets, sheet => sheet.SheetName == "Scratch" && sheet.Visibility == "very_hidden");

        var batch = await service.ParseMasterAsync(new ParseMasterImportRequest
        {
            UploadToken = inspect.UploadToken,
            SheetNames = ["Contact Support"]
        }, null);

        Assert.Equal(1, batch.SheetsDetected);
        Assert.Equal("Contact Support", Assert.Single(batch.Sheets).SheetName);
        Assert.DoesNotContain(batch.Sheets, sheet => sheet.SheetName == "Billing");
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

    private static IFormFile XlsxFile(string fileName, bool includeHiddenStates = false)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "[Content_Types].xml", """
<?xml version="1.0" encoding="UTF-8"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
  <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
</Types>
""");
            AddEntry(archive, "_rels/.rels", """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
</Relationships>
""");
            AddEntry(archive, "xl/workbook.xml", includeHiddenStates ? """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Contact Support" sheetId="1" r:id="rId1"/>
    <sheet name="Billing" sheetId="2" state="hidden" r:id="rId2"/>
    <sheet name="Scratch" sheetId="3" state="veryHidden" r:id="rId3"/>
  </sheets>
</workbook>
""" : """
<?xml version="1.0" encoding="UTF-8"?>
<workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <sheets>
    <sheet name="Contact Support" sheetId="1" r:id="rId1"/>
    <sheet name="Billing" sheetId="2" r:id="rId2"/>
  </sheets>
</workbook>
""");
            AddEntry(archive, "xl/_rels/workbook.xml.rels", includeHiddenStates ? """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
</Relationships>
""" : """
<?xml version="1.0" encoding="UTF-8"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
</Relationships>
""");
            AddEntry(archive, "xl/worksheets/sheet1.xml", WorksheetXml("TC_XLSX_001", "Contact Support", "Line one\r\nLine two"));
            AddEntry(archive, "xl/worksheets/sheet2.xml", WorksheetXml("TC_XLSX_002", "Billing", "Billing description"));
            if (includeHiddenStates) AddEntry(archive, "xl/worksheets/sheet3.xml", WorksheetXml("TC_XLSX_003", "Scratch", "Scratch description"));
        }
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    private static string WorksheetXml(string testCaseId, string module, string description)
    {
        return $$"""
<?xml version="1.0" encoding="UTF-8"?>
<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
  <sheetData>
    <row r="1">
      <c r="A1" t="inlineStr"><is><t>Test Case ID</t></is></c>
      <c r="B1" t="inlineStr"><is><t>Module/Sub Module</t></is></c>
      <c r="C1" t="inlineStr"><is><t>Type of testing</t></is></c>
      <c r="D1" t="inlineStr"><is><t>Test Case Description</t></is></c>
    </row>
    <row r="2">
      <c r="A2" t="inlineStr"><is><t>{{testCaseId}}</t></is></c>
      <c r="B2" t="inlineStr"><is><t>{{module}}</t></is></c>
      <c r="C2" t="inlineStr"><is><t>Tomcat_Reg</t></is></c>
      <c r="D2" t="inlineStr"><is><t>{{System.Security.SecurityElement.Escape(description)}}</t></is></c>
    </row>
  </sheetData>
</worksheet>
""";
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string Tsv(params string[][] rows)
    {
        return string.Join('\n', rows.Select(row => string.Join('\t', row)));
    }
}
