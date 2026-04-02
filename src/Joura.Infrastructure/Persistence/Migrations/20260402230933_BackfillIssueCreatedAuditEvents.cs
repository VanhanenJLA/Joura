using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BackfillIssueCreatedAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO "AuditEvents" ("Id", "IssueId", "ActorId", "EventType", "Description", "CreatedUtc")
                SELECT
                    (
                        substr(md5(i."Id"::text || clock_timestamp()::text), 1, 8) || '-' ||
                        substr(md5(i."Id"::text || clock_timestamp()::text), 9, 4) || '-' ||
                        substr(md5(i."Id"::text || clock_timestamp()::text), 13, 4) || '-' ||
                        substr(md5(i."Id"::text || clock_timestamp()::text), 17, 4) || '-' ||
                        substr(md5(i."Id"::text || clock_timestamp()::text), 21, 12)
                    )::uuid,
                    i."Id",
                    i."ReporterId",
                    'IssueCreated',
                    'Created issue ' || i."Key" || '.',
                    i."CreatedUtc"
                FROM "Issues" i
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "AuditEvents" a
                    WHERE a."IssueId" = i."Id"
                      AND a."EventType" = 'IssueCreated'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
