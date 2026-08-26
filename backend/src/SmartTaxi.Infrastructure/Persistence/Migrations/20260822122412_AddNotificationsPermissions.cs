using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsPermissions : Migration
    {
        private static readonly string[] AllRoles =
        [
            "Customer", "Driver", "Admin", "TaxiOwner", "GaragePartner", "RoadsideAssistancePartner", "Advertiser", "BusinessCustomer"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every authenticated role gets self-service access to its own notification center, preferences facade, and device tokens.
            foreach (var permission in new[]
                     {
                         "notifications.read.own", "notifications.preferences.manage.own", "notifications.device-tokens.manage.own"
                     })
            {
                foreach (var role in AllRoles)
                {
                    migrationBuilder.InsertData(
                        table: "RolePermissions",
                        columns: new[] { "Role", "PermissionCode" },
                        values: new object[,] { { role, permission } });
                }
            }

            // Admin: manages the template catalog and has global delivery-monitoring/retry oversight.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "notifications.templates.manage" },
                    { "Admin", "notifications.delivery.read" },
                    { "Admin", "notifications.delivery.manage" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in new[]
                     {
                         "notifications.read.own", "notifications.preferences.manage.own", "notifications.device-tokens.manage.own"
                     })
            {
                foreach (var role in AllRoles)
                {
                    migrationBuilder.DeleteData(
                        table: "RolePermissions",
                        keyColumns: new[] { "Role", "PermissionCode" },
                        keyValues: new object[] { role, permission });
                }
            }

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "notifications.templates.manage" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "notifications.delivery.read" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "notifications.delivery.manage" });
        }
    }
}
