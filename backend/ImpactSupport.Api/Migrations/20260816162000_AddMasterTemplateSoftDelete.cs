using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations;

public partial class AddMasterTemplateSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "MasterIsActive",
            table: "MasterTemplate",
            type: "INTEGER",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "MasterDeletedAt",
            table: "MasterTemplate",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MasterDeletedBy",
            table: "MasterTemplate",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MasterIsActive", table: "MasterTemplate");
        migrationBuilder.DropColumn(name: "MasterDeletedAt", table: "MasterTemplate");
        migrationBuilder.DropColumn(name: "MasterDeletedBy", table: "MasterTemplate");
    }
}
