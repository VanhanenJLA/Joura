using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetDemoCompanyTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                DECLARE target_id uuid;
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Tenants" WHERE "Key" = 'DEMO') THEN
                        UPDATE "Tenants"
                        SET "Name" = 'Demo Company'
                        WHERE "Key" = 'DEMO';
                        RETURN;
                    END IF;

                    SELECT "Id" INTO target_id
                    FROM "Tenants"
                    WHERE "Key" IN ('HOME', 'ARCH')
                       OR "Name" IN ('We have Jira at home.', 'Architecture Practice')
                    ORDER BY "CreatedUtc"
                    LIMIT 1;

                    IF target_id IS NOT NULL THEN
                        UPDATE "Tenants"
                        SET "Name" = 'Demo Company',
                            "Key" = 'DEMO'
                        WHERE "Id" = target_id;
                    END IF;
                END
                $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Tenants"
                SET "Name" = 'We have Jira at home.',
                    "Key" = 'HOME'
                WHERE "Key" = 'DEMO'
                  AND "Name" = 'Demo Company'
                  AND NOT EXISTS (SELECT 1 FROM "Tenants" WHERE "Key" = 'HOME');
                """);
        }
    }
}
