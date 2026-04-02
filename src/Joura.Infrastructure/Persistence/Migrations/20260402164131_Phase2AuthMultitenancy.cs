using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase2AuthMultitenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_Key",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Issues_Key",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ProjectId",
                table: "Issues");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "Tenants" ("Id", "Name", "Key", "CreatedUtc")
                SELECT '11111111-1111-1111-1111-111111111111'::uuid, 'Default Tenant', 'DEFAULT', NOW()
                WHERE NOT EXISTS (SELECT 1 FROM "Tenants");
                """);

            migrationBuilder.Sql("""
                UPDATE "Users"
                SET "TenantId" = (
                    SELECT "Id"
                    FROM "Tenants"
                    ORDER BY "CreatedUtc"
                    LIMIT 1
                )
                WHERE "TenantId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_Key",
                table: "Projects",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ProjectId_Key",
                table: "Issues",
                columns: new[] { "ProjectId", "Key" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Projects_TenantId_Key",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ProjectId_Key",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Key",
                table: "Projects",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Key",
                table: "Issues",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ProjectId",
                table: "Issues",
                column: "ProjectId");
        }
    }
}
