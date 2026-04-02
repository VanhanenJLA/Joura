using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Joura.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameWorkspaceToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Workspaces_WorkspaceId",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Workspaces",
                table: "Workspaces");

            migrationBuilder.RenameTable(
                name: "Workspaces",
                newName: "Tenants");

            migrationBuilder.RenameIndex(
                name: "IX_Workspaces_Key",
                table: "Tenants",
                newName: "IX_Tenants_Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants",
                column: "Id");

            migrationBuilder.RenameColumn(
                name: "WorkspaceId",
                table: "Projects",
                newName: "TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_WorkspaceId",
                table: "Projects",
                newName: "IX_Projects_TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Tenants_TenantId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                table: "Projects",
                newName: "WorkspaceId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_TenantId",
                table: "Projects",
                newName: "IX_Projects_WorkspaceId");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tenants",
                table: "Tenants");

            migrationBuilder.RenameTable(
                name: "Tenants",
                newName: "Workspaces");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_Key",
                table: "Workspaces",
                newName: "IX_Workspaces_Key");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Workspaces",
                table: "Workspaces",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Workspaces_WorkspaceId",
                table: "Projects",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
