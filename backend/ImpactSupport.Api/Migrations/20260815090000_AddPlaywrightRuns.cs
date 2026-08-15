using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    public partial class AddPlaywrightRuns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TestRunConfigFlags");
            migrationBuilder.DropTable(name: "TestRunConfigTargets");
            migrationBuilder.DropTable(name: "TestRunConfigTestingTypes");
            migrationBuilder.DropTable(name: "TestRunExecutions");
            migrationBuilder.DropTable(name: "TestRunConfigs");
        }
    }
}
