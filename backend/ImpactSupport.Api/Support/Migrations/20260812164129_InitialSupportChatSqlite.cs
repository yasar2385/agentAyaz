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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SupportMessages");

            migrationBuilder.DropTable(
                name: "SupportSessions");
        }
    }
}
