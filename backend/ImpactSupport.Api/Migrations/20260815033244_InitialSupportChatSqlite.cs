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
                    LastDriveCheckedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastMetadataSyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastLocalSyncAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastGoogleUpdateAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    FolderUrl = table.Column<string>(type: "TEXT", nullable: false),
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
                name: "QaImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UploadKind = table.Column<string>(type: "TEXT", nullable: false),
                    ResultMode = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    UploadedBy = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RowsAdded = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsUpdated = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsSkipped = table.Column<int>(type: "INTEGER", nullable: false),
                    RowsError = table.Column<int>(type: "INTEGER", nullable: false),
                    SheetsDetected = table.Column<int>(type: "INTEGER", nullable: false),
                    NewSheets = table.Column<int>(type: "INTEGER", nullable: false),
                    ExistingSheets = table.Column<int>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CommittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaImportBatches", x => x.Id);
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
                    SheetIndex = table.Column<int>(type: "INTEGER", nullable: true),
                    SheetGid = table.Column<int>(type: "INTEGER", nullable: true),
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
                    LastMetadataSyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
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
                name: "QaImportBatchErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    RowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RawValue = table.Column<string>(type: "TEXT", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaImportBatchErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaImportBatchErrors_QaImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "QaImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QaImportBatchSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    SheetName = table.Column<string>(type: "TEXT", nullable: false),
                    NormalizedSheetName = table.Column<string>(type: "TEXT", nullable: false),
                    ModuleName = table.Column<string>(type: "TEXT", nullable: false),
                    RowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SelectedAction = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaImportBatchSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaImportBatchSheets_QaImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "QaImportBatches",
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

            migrationBuilder.CreateTable(
                name: "QaImportBatchRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImportBatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    ImportBatchSheetId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    TestCaseId = table.Column<string>(type: "TEXT", nullable: false),
                    RowJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QaImportBatchRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QaImportBatchRows_QaImportBatchSheets_ImportBatchSheetId",
                        column: x => x.ImportBatchSheetId,
                        principalTable: "QaImportBatchSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QaImportBatchRows_QaImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "QaImportBatches",
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

            migrationBuilder.CreateIndex(
                name: "IX_QaImportBatchErrors_ImportBatchId",
                table: "QaImportBatchErrors",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_QaImportBatches_UploadKind_Status",
                table: "QaImportBatches",
                columns: new[] { "UploadKind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_QaImportBatchRows_ImportBatchId_TestCaseId",
                table: "QaImportBatchRows",
                columns: new[] { "ImportBatchId", "TestCaseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QaImportBatchRows_ImportBatchSheetId",
                table: "QaImportBatchRows",
                column: "ImportBatchSheetId");

            migrationBuilder.CreateIndex(
                name: "IX_QaImportBatchSheets_ImportBatchId_NormalizedSheetName",
                table: "QaImportBatchSheets",
                columns: new[] { "ImportBatchId", "NormalizedSheetName" },
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
                name: "QaImportBatchErrors");

            migrationBuilder.DropTable(
                name: "QaImportBatchRows");

            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "TestCaseViewerUsers");

            migrationBuilder.DropTable(
                name: "QaDashboardFileCaches");

            migrationBuilder.DropTable(
                name: "QaImportBatchSheets");

            migrationBuilder.DropTable(
                name: "SupportSessions");

            migrationBuilder.DropTable(
                name: "QaImportBatches");
        }
    }
}
