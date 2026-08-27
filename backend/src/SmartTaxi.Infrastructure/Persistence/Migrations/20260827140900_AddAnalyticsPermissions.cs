using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsPermissions : Migration
    {
        /// <summary>Admin-only — Analytics has no "own" concept, unlike Support's 7-permission own/all split.</summary>
        private static readonly string[] AdminPermissions =
        [
            "analytics.dashboard.read", "analytics.reports.read", "analytics.reports.export"
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
