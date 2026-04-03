using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorklogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorklogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoggedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Minutes = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorklogEntries", x => x.Id);
                    table.CheckConstraint("CK_WorklogEntries_Minutes_Positive", "\"Minutes\" > 0");
                    table.ForeignKey(
                        name: "FK_WorklogEntries_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorklogEntries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_IssueId_LoggedDate",
                table: "WorklogEntries",
                columns: new[] { "IssueId", "LoggedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_UserId_LoggedDate",
                table: "WorklogEntries",
                columns: new[] { "UserId", "LoggedDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorklogEntries");
        }
    }
}
