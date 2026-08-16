using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations;

public partial class AddMasterReviewEditHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MasterUpdatedBy",
            table: "MasterTemplate",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "ManualEditConflict",
            table: "QaImportBatchRows",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "ManualEditAction",
            table: "QaImportBatchRows",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "ManualEditLastEditedBy",
            table: "QaImportBatchRows",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ManualEditLastEditedAt",
            table: "QaImportBatchRows",
            type: "TEXT",
            nullable: true);

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

        migrationBuilder.CreateIndex(
            name: "IX_MasterTemplateEditHistory_MasterId_EditedAt",
            table: "MasterTemplateEditHistory",
            columns: new[] { "MasterId", "EditedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MasterTemplateEditHistory");
        migrationBuilder.DropColumn(name: "MasterUpdatedBy", table: "MasterTemplate");
        migrationBuilder.DropColumn(name: "ManualEditConflict", table: "QaImportBatchRows");
        migrationBuilder.DropColumn(name: "ManualEditAction", table: "QaImportBatchRows");
        migrationBuilder.DropColumn(name: "ManualEditLastEditedBy", table: "QaImportBatchRows");
        migrationBuilder.DropColumn(name: "ManualEditLastEditedAt", table: "QaImportBatchRows");
    }
}
