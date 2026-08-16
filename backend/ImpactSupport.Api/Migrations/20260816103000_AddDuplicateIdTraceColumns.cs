using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations;

public partial class AddDuplicateIdTraceColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MasterOriginalRawId",
            table: "MasterTemplate",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OriginalRawTestCaseId",
            table: "QaImportBatchRows",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MasterOriginalRawId",
            table: "MasterTemplate");

        migrationBuilder.DropColumn(
            name: "OriginalRawTestCaseId",
            table: "QaImportBatchRows");
    }
}
