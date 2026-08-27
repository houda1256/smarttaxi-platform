using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrationPermissions : Migration
    {
        /// <summary>Admin-only — no new UserRole, no ".own" tier; two-factor reset is separated from general user management due to elevated risk.</summary>
        private static readonly string[] AdminPermissions =
        [
            "admin.users.manage", "admin.users.two-factor.reset", "admin.audit-log.read"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Admin", permission } });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }
        }
    }
}
