using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledWorklogIntervals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorklogEntries_IssueId_LoggedDate",
                table: "WorklogEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorklogEntries_UserId_LoggedDate",
                table: "WorklogEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorklogEntries_Minutes_Positive",
                table: "WorklogEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndAt",
                table: "WorklogEntries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartAt",
                table: "WorklogEntries",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "WorklogEntries"
                SET "StartAt" = "LoggedDate"::timestamp + time '09:00',
                    "EndAt" = ("LoggedDate"::timestamp + time '09:00') + make_interval(mins => "Minutes");
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndAt",
                table: "WorklogEntries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartAt",
                table: "WorklogEntries",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "LoggedDate",
                table: "WorklogEntries");

            migrationBuilder.DropColumn(
                name: "Minutes",
                table: "WorklogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_IssueId_StartAt",
                table: "WorklogEntries",
                columns: new[] { "IssueId", "StartAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_UserId_StartAt",
                table: "WorklogEntries",
                columns: new[] { "UserId", "StartAt" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorklogEntries_Duration_Positive",
                table: "WorklogEntries",
                sql: "\"EndAt\" > \"StartAt\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorklogEntries_IssueId_StartAt",
                table: "WorklogEntries");

            migrationBuilder.DropIndex(
                name: "IX_WorklogEntries_UserId_StartAt",
                table: "WorklogEntries");

            migrationBuilder.DropCheckConstraint(
                name: "CK_WorklogEntries_Duration_Positive",
                table: "WorklogEntries");

            migrationBuilder.AddColumn<DateOnly>(
                name: "LoggedDate",
                table: "WorklogEntries",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Minutes",
                table: "WorklogEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "WorklogEntries"
                SET "LoggedDate" = "StartAt"::date,
                    "Minutes" = GREATEST(1, CAST(EXTRACT(EPOCH FROM ("EndAt" - "StartAt")) / 60 AS integer));
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "LoggedDate",
                table: "WorklogEntries",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Minutes",
                table: "WorklogEntries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "EndAt",
                table: "WorklogEntries");

            migrationBuilder.DropColumn(
                name: "StartAt",
                table: "WorklogEntries");

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_IssueId_LoggedDate",
                table: "WorklogEntries",
                columns: new[] { "IssueId", "LoggedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WorklogEntries_UserId_LoggedDate",
                table: "WorklogEntries",
                columns: new[] { "UserId", "LoggedDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_WorklogEntries_Minutes_Positive",
                table: "WorklogEntries",
                sql: "\"Minutes\" > 0");
        }
    }
}
