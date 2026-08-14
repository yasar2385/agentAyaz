using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupportChatSqlite : Migration
    {
        /// <inheritdoc />
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
                name: "SupportSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SupportSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    TicketNo = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", nullable: true),
                    UserRole = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<string>(type: "TEXT", nullable: false),
                    DocumentLink = table.Column<string>(type: "TEXT", nullable: true),
                    ImpactSessionId = table.Column<string>(type: "TEXT", nullable: true),
                    ModuleName = table.Column<string>(type: "TEXT", nullable: true),
                    ClientName = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClosedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportSessions", x => x.Id);
                    table.UniqueConstraint("AK_SupportSessions_SupportSessionId", x => x.SupportSessionId);
                });

            migrationBuilder.CreateTable(
                name: "TestCaseViewerUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MongoId = table.Column<string>(type: "TEXT", nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    RoleJson = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCaseViewerUsers", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "SupportMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    SupportSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    SenderUserId = table.Column<string>(type: "TEXT", nullable: false),
                    SenderName = table.Column<string>(type: "TEXT", nullable: true),
                    SenderRole = table.Column<string>(type: "TEXT", nullable: true),
                    MessageText = table.Column<string>(type: "TEXT", nullable: false),
                    MessageType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportMessages_SupportSessions_SupportSessionId",
                        column: x => x.SupportSessionId,
                        principalTable: "SupportSessions",
                        principalColumn: "SupportSessionId",
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

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_MessageId",
                table: "SupportMessages",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportMessages_SupportSessionId",
                table: "SupportMessages",
                column: "SupportSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_SupportSessionId",
                table: "SupportSessions",
                column: "SupportSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_TicketNo",
                table: "SupportSessions",
                column: "TicketNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportSessions_UserId_DocumentId_UserRole_Status",
                table: "SupportSessions",
                columns: new[] { "UserId", "DocumentId", "UserRole", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseViewerUsers_MongoId",
                table: "TestCaseViewerUsers",
                column: "MongoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCaseViewerUsers_Username",
                table: "TestCaseViewerUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QaDashboardSheetCaches");

            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "TestCaseViewerUsers");

            migrationBuilder.DropTable(
                name: "QaDashboardFileCaches");

            migrationBuilder.DropTable(
                name: "SupportSessions");
        }
    }
}
