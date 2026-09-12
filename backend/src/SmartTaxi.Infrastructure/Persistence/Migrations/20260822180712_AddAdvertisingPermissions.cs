using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvertisingPermissions : Migration
    {
        private static readonly string[] AdvertiserPermissions =
        [
            "advertising.profile.manage.own", "advertising.campaigns.read.own", "advertising.campaigns.manage.own",
            "advertising.campaigns.submit.own", "advertising.performance.read.own"
        ];

        private static readonly string[] AdminPermissions =
        [
            "advertising.campaigns.review", "advertising.campaigns.read.all", "advertising.placements.manage",
            "advertising.performance.read.all"
        ];

        // Consumer-side apps (rider/driver) actually view/click a served ad — not the advertiser managing the campaign.
        private static readonly string[] TrackingRoles = ["Customer", "Driver"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in AdvertiserPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Advertiser", permission } });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Admin", permission } });
            }

            foreach (var role in TrackingRoles)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" },
                    values: new object[,] { { role, "advertising.tracking.record" } });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in AdvertiserPermissions)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Advertiser", permission });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }

            foreach (var role in TrackingRoles)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { role, "advertising.tracking.record" });
            }
        }
    }
}
