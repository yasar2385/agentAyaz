using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ImpactSupport.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialSupportChatSqlite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientAliases",
                columns: table => new
                {
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientAliases", x => x.Alias);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DtdType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DtdType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterDevStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterDevStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterIssueTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterIssueTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterPreconditionRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterPreconditionRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterQaStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterQaStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterTemplate",
                columns: table => new
                {
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterTestId = table.Column<string>(type: "TEXT", nullable: false),
                    MasterOriginalRawId = table.Column<string>(type: "TEXT", nullable: true),
                    MasterTestNo = table.Column<string>(type: "TEXT", nullable: false),
                    MasterSourceSheet = table.Column<string>(type: "TEXT", nullable: false),
                    MasterSourceRow = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterModules = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterPreconditionRole = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterClient = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterType = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterDtdType = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterRoleWorkflow = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterIsCollaborative = table.Column<bool>(type: "INTEGER", nullable: false),
                    MasterIsSharedRole = table.Column<bool>(type: "INTEGER", nullable: false),
                    MasterPreparedBy = table.Column<string>(type: "TEXT", nullable: false),
                    MasterPreparedDate = table.Column<string>(type: "TEXT", nullable: false),
                    MasterTestData = table.Column<string>(type: "TEXT", nullable: false),
                    MasterExpectedResult = table.Column<string>(type: "TEXT", nullable: false),
                    MasterActualResult = table.Column<string>(type: "TEXT", nullable: false),
                    MasterIssueType = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterQaStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterDevStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterCreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MasterUpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    MasterUpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTemplate", x => x.MasterId);
                });

            migrationBuilder.CreateTable(
                name: "MasterTestingTypeAliases",
                columns: table => new
                {
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    TestingTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTestingTypeAliases", x => x.Alias);
                });

            migrationBuilder.CreateTable(
                name: "MasterTestingTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTestingTypes", x => x.Id);
                });

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
                name: "RefStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleWorkflows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleWorkflows", x => x.Id);
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
                name: "TestingMetaResultLinks",
                columns: table => new
                {
                    TestingMetaResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestingUrlId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingMetaResultLinks", x => new { x.TestingMetaResultId, x.RoleId });
                });

            migrationBuilder.CreateTable(
                name: "TestingMetaResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RunBy = table.Column<int>(type: "INTEGER", nullable: true),
                    RunAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RunThrough = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingMetaResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestingMetaResultTestingTypes",
                columns: table => new
                {
                    TestingMetaResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestingTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingMetaResultTestingTypes", x => new { x.TestingMetaResultId, x.TestingTypeId });
                });

            migrationBuilder.CreateTable(
                name: "TestingUrls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    UrlType = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingUrls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TestRunConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestingName = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Types",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MasterTemplateClients",
                columns: table => new
                {
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTemplateClients", x => new { x.MasterId, x.ClientId });
                    table.ForeignKey(
                        name: "FK_MasterTemplateClients_MasterTemplate_MasterId",
                        column: x => x.MasterId,
                        principalTable: "MasterTemplate",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterTemplateEditHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", nullable: false),
                    EditedBy = table.Column<string>(type: "TEXT", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTemplateEditHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterTemplateEditHistory_MasterTemplate_MasterId",
                        column: x => x.MasterId,
                        principalTable: "MasterTemplate",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterTemplateRemarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    RoundNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    QaRemark = table.Column<string>(type: "TEXT", nullable: false),
                    DevRemark = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTemplateRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MasterTemplateRemarks_MasterTemplate_MasterId",
                        column: x => x.MasterId,
                        principalTable: "MasterTemplate",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterTemplateTestingTypes",
                columns: table => new
                {
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    TestingTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTemplateTestingTypes", x => new { x.MasterId, x.TestingTypeId });
                    table.ForeignKey(
                        name: "FK_MasterTemplateTestingTypes_MasterTemplate_MasterId",
                        column: x => x.MasterId,
                        principalTable: "MasterTemplate",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MasterTestDetails",
                columns: table => new
                {
                    MasterId = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterDescription = table.Column<string>(type: "TEXT", nullable: false),
                    MasterTestSteps = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MasterTestDetails", x => x.MasterId);
                    table.ForeignKey(
                        name: "FK_MasterTestDetails_MasterTemplate_MasterId",
                        column: x => x.MasterId,
                        principalTable: "MasterTemplate",
                        principalColumn: "MasterId",
                        onDelete: ReferentialAction.Cascade);
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
                name: "TestingDataResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TestingMetaResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterTestId = table.Column<string>(type: "TEXT", nullable: false),
                    MasterIssueType = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterQaStatus = table.Column<int>(type: "INTEGER", nullable: true),
                    MasterDevStatus = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingDataResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestingDataResults_TestingMetaResults_TestingMetaResultId",
                        column: x => x.TestingMetaResultId,
                        principalTable: "TestingMetaResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestingMetaResultModuleStats",
                columns: table => new
                {
                    TestingMetaResultId = table.Column<int>(type: "INTEGER", nullable: false),
                    MasterModuleId = table.Column<int>(type: "INTEGER", nullable: false),
                    PassCount = table.Column<int>(type: "INTEGER", nullable: false),
                    FailCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestingMetaResultModuleStats", x => new { x.TestingMetaResultId, x.MasterModuleId });
                    table.ForeignKey(
                        name: "FK_TestingMetaResultModuleStats_TestingMetaResults_TestingMetaResultId",
                        column: x => x.TestingMetaResultId,
                        principalTable: "TestingMetaResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRunConfigFlags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlagKey = table.Column<string>(type: "TEXT", nullable: false),
                    FlagValue = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunConfigFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRunConfigFlags_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRunConfigTargets",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    ModuleName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunConfigTargets", x => new { x.ConfigId, x.ModuleName });
                    table.ForeignKey(
                        name: "FK_TestRunConfigTargets_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRunConfigTestingTypes",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunConfigTestingTypes", x => new { x.ConfigId, x.Value });
                    table.ForeignKey(
                        name: "FK_TestRunConfigTestingTypes_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRunConfigWorkflowContexts",
                columns: table => new
                {
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    Client = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Domain = table.Column<string>(type: "TEXT", nullable: false),
                    RoleWorkflow = table.Column<string>(type: "TEXT", nullable: false),
                    TestingUrl = table.Column<string>(type: "TEXT", nullable: false),
                    MantisTicket = table.Column<string>(type: "TEXT", nullable: false),
                    RefStyle = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunConfigWorkflowContexts", x => x.ConfigId);
                    table.ForeignKey(
                        name: "FK_TestRunConfigWorkflowContexts_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestRunExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    TriggeredBy = table.Column<string>(type: "TEXT", nullable: false),
                    TriggeredAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    PlaywrightCommand = table.Column<string>(type: "TEXT", nullable: false),
                    PlaywrightTestsRef = table.Column<string>(type: "TEXT", nullable: false),
                    ReportPath = table.Column<string>(type: "TEXT", nullable: false),
                    RunKind = table.Column<string>(type: "TEXT", nullable: false),
                    ModuleName = table.Column<string>(type: "TEXT", nullable: false),
                    TestCaseId = table.Column<string>(type: "TEXT", nullable: false),
                    MantisTicket = table.Column<string>(type: "TEXT", nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: true),
                    FailureSummary = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRunExecutions_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
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
                    OriginalRawTestCaseId = table.Column<string>(type: "TEXT", nullable: true),
                    ManualEditConflict = table.Column<bool>(type: "INTEGER", nullable: false),
                    ManualEditAction = table.Column<string>(type: "TEXT", nullable: false),
                    ManualEditLastEditedBy = table.Column<string>(type: "TEXT", nullable: false),
                    ManualEditLastEditedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "TestRunProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConfigId = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LastModuleName = table.Column<string>(type: "TEXT", nullable: false),
                    LastExecutionId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestRunProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestRunProgresses_TestRunConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalTable: "TestRunConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestRunProgresses_TestRunExecutions_LastExecutionId",
                        column: x => x.LastExecutionId,
                        principalTable: "TestRunExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "ClientAliases",
                columns: new[] { "Alias", "ClientId" },
                values: new object[,]
                {
                    { "OxfordMed", 2 },
                    { "T & F", 3 },
                    { "T&F", 3 }
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Code", "Name" },
                values: new object[,]
                {
                    { 1, "OSO", "OSO" },
                    { 2, "OXMEDO", "OxfordMed" },
                    { 3, "TNF", "T & F" }
                });

            migrationBuilder.InsertData(
                table: "DtdType",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "JATS" },
                    { 2, "BITS" },
                    { 3, "DOCBOOK" }
                });

            migrationBuilder.InsertData(
                table: "MasterDevStatuses",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Fixed" },
                    { 2, "Rejected" },
                    { 3, "WIP" },
                    { 4, "Open" }
                });

            migrationBuilder.InsertData(
                table: "MasterIssueTypes",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Bug" },
                    { 2, "Change Request" },
                    { 3, "Enhancement" }
                });

            migrationBuilder.InsertData(
                table: "MasterPreconditionRoles",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Author" },
                    { 2, "PE" },
                    { 3, "Collator" },
                    { 4, "Editor" }
                });

            migrationBuilder.InsertData(
                table: "MasterQaStatuses",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Pass" },
                    { 2, "Fail" },
                    { 3, "Fixed" },
                    { 4, "Rejected" },
                    { 5, "WIP" }
                });

            migrationBuilder.InsertData(
                table: "MasterTestingTypeAliases",
                columns: new[] { "Alias", "TestingTypeId" },
                values: new object[] { "Tomcat_Regression", 5 });

            migrationBuilder.InsertData(
                table: "MasterTestingTypes",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Basic" },
                    { 2, "Mock" },
                    { 3, "Browser" },
                    { 4, "Regression" },
                    { 5, "Tomcat_Reg" }
                });

            migrationBuilder.InsertData(
                table: "RoleWorkflows",
                columns: new[] { "Id", "IsDefault", "Value" },
                values: new object[,]
                {
                    { 1, true, "Author_Editor_Collator" },
                    { 2, false, "Editor_Author_Collator" },
                    { 3, false, "Author_Collator" },
                    { 4, false, "Editor_Collator" }
                });

            migrationBuilder.InsertData(
                table: "TestingUrls",
                columns: new[] { "Id", "UrlType", "Value" },
                values: new object[,]
                {
                    { 1, "single", "author" },
                    { 2, "single", "editor" },
                    { 3, "single", "collator" },
                    { 4, "multi_author", "shared_author" },
                    { 5, "multi_author", "shared_editor" },
                    { 6, "multi_author", "shared_collator" }
                });

            migrationBuilder.InsertData(
                table: "Types",
                columns: new[] { "Id", "Value" },
                values: new object[,]
                {
                    { 1, "Journal" },
                    { 2, "Book" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Code",
                table: "Clients",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DtdType_Value",
                table: "DtdType",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterDevStatuses_Value",
                table: "MasterDevStatuses",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterIssueTypes_Value",
                table: "MasterIssueTypes",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterModules_Name",
                table: "MasterModules",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterPreconditionRoles_Value",
                table: "MasterPreconditionRoles",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterQaStatuses_Value",
                table: "MasterQaStatuses",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterTemplate_MasterTestId",
                table: "MasterTemplate",
                column: "MasterTestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MasterTemplateEditHistory_MasterId_EditedAt",
                table: "MasterTemplateEditHistory",
                columns: new[] { "MasterId", "EditedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MasterTemplateRemarks_MasterId",
                table: "MasterTemplateRemarks",
                column: "MasterId");

            migrationBuilder.CreateIndex(
                name: "IX_MasterTestingTypes_Value",
                table: "MasterTestingTypes",
                column: "Value",
                unique: true);

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
                name: "IX_RefStyles_Value",
                table: "RefStyles",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleWorkflows_Value",
                table: "RoleWorkflows",
                column: "Value",
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

            migrationBuilder.CreateIndex(
                name: "IX_TestingDataResults_TestingMetaResultId",
                table: "TestingDataResults",
                column: "TestingMetaResultId");

            migrationBuilder.CreateIndex(
                name: "IX_TestingUrls_Value",
                table: "TestingUrls",
                column: "Value",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRunConfigFlags_ConfigId_FlagKey",
                table: "TestRunConfigFlags",
                columns: new[] { "ConfigId", "FlagKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRunConfigs_TestingName",
                table: "TestRunConfigs",
                column: "TestingName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRunExecutions_ConfigId_Status",
                table: "TestRunExecutions",
                columns: new[] { "ConfigId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestRunProgresses_ConfigId_UserId",
                table: "TestRunProgresses",
                columns: new[] { "ConfigId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRunProgresses_LastExecutionId",
                table: "TestRunProgresses",
                column: "LastExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_Types_Value",
                table: "Types",
                column: "Value",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientAliases");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "DtdType");

            migrationBuilder.DropTable(
                name: "MasterDevStatuses");

            migrationBuilder.DropTable(
                name: "MasterIssueTypes");

            migrationBuilder.DropTable(
                name: "MasterModules");

            migrationBuilder.DropTable(
                name: "MasterPreconditionRoles");

            migrationBuilder.DropTable(
                name: "MasterQaStatuses");

            migrationBuilder.DropTable(
                name: "MasterTemplateClients");

            migrationBuilder.DropTable(
                name: "MasterTemplateEditHistory");

            migrationBuilder.DropTable(
                name: "MasterTemplateRemarks");

            migrationBuilder.DropTable(
                name: "MasterTemplateTestingTypes");

            migrationBuilder.DropTable(
                name: "MasterTestDetails");

            migrationBuilder.DropTable(
                name: "MasterTestingTypeAliases");

            migrationBuilder.DropTable(
                name: "MasterTestingTypes");

            migrationBuilder.DropTable(
                name: "QaDashboardSheetCaches");

            migrationBuilder.DropTable(
                name: "QaImportBatchErrors");

            migrationBuilder.DropTable(
                name: "QaImportBatchRows");

            migrationBuilder.DropTable(
                name: "RefStyles");

            migrationBuilder.DropTable(
                name: "RoleWorkflows");

            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "TestCaseViewerUsers");

            migrationBuilder.DropTable(
                name: "TestingDataResults");

            migrationBuilder.DropTable(
                name: "TestingMetaResultLinks");

            migrationBuilder.DropTable(
                name: "TestingMetaResultModuleStats");

            migrationBuilder.DropTable(
                name: "TestingMetaResultTestingTypes");

            migrationBuilder.DropTable(
                name: "TestingUrls");

            migrationBuilder.DropTable(
                name: "TestRunConfigFlags");

            migrationBuilder.DropTable(
                name: "TestRunConfigTargets");

            migrationBuilder.DropTable(
                name: "TestRunConfigTestingTypes");

            migrationBuilder.DropTable(
                name: "TestRunConfigWorkflowContexts");

            migrationBuilder.DropTable(
                name: "TestRunProgresses");

            migrationBuilder.DropTable(
                name: "Types");

            migrationBuilder.DropTable(
                name: "MasterTemplate");

            migrationBuilder.DropTable(
                name: "QaDashboardFileCaches");

            migrationBuilder.DropTable(
                name: "QaImportBatchSheets");

            migrationBuilder.DropTable(
                name: "SupportSessions");

            migrationBuilder.DropTable(
                name: "TestingMetaResults");

            migrationBuilder.DropTable(
                name: "TestRunExecutions");

            migrationBuilder.DropTable(
                name: "QaImportBatches");

            migrationBuilder.DropTable(
                name: "TestRunConfigs");
        }
    }
}
