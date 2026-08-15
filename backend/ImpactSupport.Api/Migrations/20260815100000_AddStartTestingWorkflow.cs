using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    public partial class AddStartTestingWorkflow : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MantisTicket",
                table: "TestRunExecutions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModuleName",
                table: "TestRunExecutions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RunKind",
                table: "TestRunExecutions",
                type: "TEXT",
                nullable: false,
                defaultValue: "STANDARD");

            migrationBuilder.AddColumn<string>(
                name: "TestCaseId",
                table: "TestRunExecutions",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.CreateIndex(
                name: "IX_TestRunProgresses_ConfigId_UserId",
                table: "TestRunProgresses",
                columns: new[] { "ConfigId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestRunProgresses_LastExecutionId",
                table: "TestRunProgresses",
                column: "LastExecutionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TestRunConfigWorkflowContexts");
            migrationBuilder.DropTable(name: "TestRunProgresses");

            migrationBuilder.DropColumn(name: "MantisTicket", table: "TestRunExecutions");
            migrationBuilder.DropColumn(name: "ModuleName", table: "TestRunExecutions");
            migrationBuilder.DropColumn(name: "RunKind", table: "TestRunExecutions");
            migrationBuilder.DropColumn(name: "TestCaseId", table: "TestRunExecutions");
        }
    }
}
