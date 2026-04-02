using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkIssuePriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Issues"
                SET "Priority" = CASE
                    WHEN "Priority" IN (1, 2) THEN 1
                    WHEN "Priority" = 3 THEN 2
                    WHEN "Priority" IN (4, 5) THEN 3
                    ELSE 2
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Issues"
                SET "Priority" = CASE
                    WHEN "Priority" = 1 THEN 2
                    WHEN "Priority" = 2 THEN 3
                    WHEN "Priority" = 3 THEN 4
                    ELSE 3
                END;
                """);
        }
    }
}
