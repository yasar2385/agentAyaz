using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    public partial class AddQaDashboardCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QaDashboardFileCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileId = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ReportType = table.Column<string>(type: "TEXT", nullable: false),
                    LastScannedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DriveModifiedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastLocalSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastGoogleUpdateAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ScanStatus = table.Column<string>(type: "TEXT", nullable: false),
                    ScanError = table.Column<string>(type: "TEXT", nullable: false),
                    LocalTsvPath = table.Column<string>(type: "TEXT", nullable: false),
                    PendingEditCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SyncError = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaDashboardFileCaches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QaDashboardSheetCaches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileCacheId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: false),
                    SheetName = table.Column<string>(type: "TEXT", nullable: false),
                    Module = table.Column<string>(type: "TEXT", nullable: false),
                    TotalTestCases = table.Column<int>(type: "INTEGER", nullable: false),
                    PassCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FixedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RejectedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PostponedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NotReplicateCount = table.Column<int>(type: "INTEGER", nullable: false),
                    WipCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NotClearCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FutureDevelopmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    PurposeOfTesting = table.Column<string>(type: "TEXT", nullable: false),
                    DevStatus = table.Column<string>(type: "TEXT", nullable: false),
                    DevRemarks = table.Column<string>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", nullable: false),
                    SheetLink = table.Column<string>(type: "TEXT", nullable: false),
                    Link = table.Column<string>(type: "TEXT", nullable: false),
                    RowsJson = table.Column<string>(type: "TEXT", nullable: false),
                    LastRefreshedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DriveModifiedTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastLocalSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastGoogleUpdateAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LocalTsvPath = table.Column<string>(type: "TEXT", nullable: false),
                    PendingEditCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SyncError = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshStatus = table.Column<string>(type: "TEXT", nullable: false),
                    RefreshError = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaDashboardSheetCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaDashboardSheetCaches_QaDashboardFileCaches_FileCacheId",
                        column: x => x.FileCacheId,
                        principalTable: "QaDashboardFileCaches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QaDashboardFileCaches_ReportType_FileId",
                table: "QaDashboardFileCaches",
                columns: new[] { "ReportType", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaDashboardSheetCaches_FileCacheId",
                table: "QaDashboardSheetCaches",
                column: "FileCacheId");

            migrationBuilder.CreateIndex(
                name: "IX_QaDashboardSheetCaches_FileId_SheetName",
                table: "QaDashboardSheetCaches",
                columns: new[] { "FileId", "SheetName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "QaDashboardSheetCaches");
            migrationBuilder.DropTable(name: "QaDashboardFileCaches");
        }
    }
}
