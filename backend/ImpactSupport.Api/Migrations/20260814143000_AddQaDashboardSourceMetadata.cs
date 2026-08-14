using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ImpactSupport.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQaDashboardSourceMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FolderUrl",
                table: "QaDashboardFileCaches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastDriveCheckedAt",
                table: "QaDashboardFileCaches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMetadataSyncedAt",
                table: "QaDashboardFileCaches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "QaDashboardFileCaches",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastMetadataSyncedAt",
                table: "QaDashboardSheetCaches",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SheetGid",
                table: "QaDashboardSheetCaches",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SheetIndex",
                table: "QaDashboardSheetCaches",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FolderUrl", table: "QaDashboardFileCaches");
            migrationBuilder.DropColumn(name: "LastDriveCheckedAt", table: "QaDashboardFileCaches");
            migrationBuilder.DropColumn(name: "LastMetadataSyncedAt", table: "QaDashboardFileCaches");
            migrationBuilder.DropColumn(name: "SourceUrl", table: "QaDashboardFileCaches");
            migrationBuilder.DropColumn(name: "LastMetadataSyncedAt", table: "QaDashboardSheetCaches");
            migrationBuilder.DropColumn(name: "SheetGid", table: "QaDashboardSheetCaches");
            migrationBuilder.DropColumn(name: "SheetIndex", table: "QaDashboardSheetCaches");
        }
    }
}
