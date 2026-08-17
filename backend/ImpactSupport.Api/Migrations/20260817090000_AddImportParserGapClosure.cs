using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations;

public partial class AddImportParserGapClosure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "MasterSubClient",
            table: "MasterTemplate",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "ClientSubBrands",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                Value = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ClientSubBrands", x => x.Id);
                table.ForeignKey(
                    name: "FK_ClientSubBrands_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "MasterPreconditionRoleAliases",
            columns: table => new
            {
                Alias = table.Column<string>(type: "TEXT", nullable: false),
                RoleId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MasterPreconditionRoleAliases", x => x.Alias);
                table.ForeignKey(
                    name: "FK_MasterPreconditionRoleAliases_MasterPreconditionRoles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "MasterPreconditionRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "TypeClientDtdMap",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TypeId = table.Column<int>(type: "INTEGER", nullable: false),
                ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                SubClientId = table.Column<int>(type: "INTEGER", nullable: true),
                DtdTypeId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TypeClientDtdMap", x => x.Id);
                table.ForeignKey(
                    name: "FK_TypeClientDtdMap_Clients_ClientId",
                    column: x => x.ClientId,
                    principalTable: "Clients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TypeClientDtdMap_ClientSubBrands_SubClientId",
                    column: x => x.SubClientId,
                    principalTable: "ClientSubBrands",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TypeClientDtdMap_DtdType_DtdTypeId",
                    column: x => x.DtdTypeId,
                    principalTable: "DtdType",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_TypeClientDtdMap_Types_TypeId",
                    column: x => x.TypeId,
                    principalTable: "Types",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ClientSubBrands_ClientId_Value",
            table: "ClientSubBrands",
            columns: new[] { "ClientId", "Value" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MasterPreconditionRoleAliases_RoleId",
            table: "MasterPreconditionRoleAliases",
            column: "RoleId");

        migrationBuilder.CreateIndex(
            name: "IX_TypeClientDtdMap_ClientId",
            table: "TypeClientDtdMap",
            column: "ClientId");

        migrationBuilder.CreateIndex(
            name: "IX_TypeClientDtdMap_DtdTypeId",
            table: "TypeClientDtdMap",
            column: "DtdTypeId");

        migrationBuilder.CreateIndex(
            name: "IX_TypeClientDtdMap_SubClientId",
            table: "TypeClientDtdMap",
            column: "SubClientId");

        migrationBuilder.CreateIndex(
            name: "IX_TypeClientDtdMap_TypeId_ClientId_SubClientId",
            table: "TypeClientDtdMap",
            columns: new[] { "TypeId", "ClientId", "SubClientId" },
            unique: true);

        migrationBuilder.InsertData(
            table: "Clients",
            columns: new[] { "Id", "Code", "Name" },
            values: new object[,]
            {
                { 4, "OUP", "OUP" },
                { 5, "LWW", "LWW" },
                { 6, "OHO", "OHO" }
            });

        migrationBuilder.InsertData(
            table: "ClientSubBrands",
            columns: new[] { "Id", "ClientId", "Value" },
            values: new object[] { 1, 5, "Thomson" });

        migrationBuilder.InsertData(
            table: "MasterPreconditionRoleAliases",
            columns: new[] { "Alias", "RoleId" },
            values: new object[] { "PE", 4 });

        migrationBuilder.InsertData(
            table: "TypeClientDtdMap",
            columns: new[] { "Id", "TypeId", "ClientId", "SubClientId", "DtdTypeId" },
            values: new object[,]
            {
                { 1, 1, 4, null, 1 },
                { 2, 1, 5, null, 1 },
                { 3, 1, 5, 1, 1 },
                { 4, 2, 1, null, 2 },
                { 5, 2, 6, null, 2 },
                { 6, 2, 2, null, 2 }
            });

        migrationBuilder.Sql("UPDATE MasterTemplate SET MasterPreconditionRole = 4 WHERE MasterPreconditionRole = 2");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ClientSubBrands");
        migrationBuilder.DropTable(name: "MasterPreconditionRoleAliases");
        migrationBuilder.DropTable(name: "TypeClientDtdMap");
        migrationBuilder.DropColumn(name: "MasterSubClient", table: "MasterTemplate");
        migrationBuilder.DeleteData(table: "Clients", keyColumn: "Id", keyValue: 4);
        migrationBuilder.DeleteData(table: "Clients", keyColumn: "Id", keyValue: 5);
        migrationBuilder.DeleteData(table: "Clients", keyColumn: "Id", keyValue: 6);
    }
}
