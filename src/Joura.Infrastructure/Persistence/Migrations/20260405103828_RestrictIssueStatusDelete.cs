using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestrictIssueStatusDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues",
                column: "StatusId",
                principalTable: "IssueStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues");

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_IssueStatuses_StatusId",
                table: "Issues",
                column: "StatusId",
                principalTable: "IssueStatuses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
