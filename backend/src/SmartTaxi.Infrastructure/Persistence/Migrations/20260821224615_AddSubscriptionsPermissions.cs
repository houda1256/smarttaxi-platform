using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionsPermissions : Migration
    {
        private static readonly string[] SubscriberRoles =
        [
            "Customer", "Driver", "TaxiOwner", "GaragePartner", "RoadsideAssistancePartner", "Advertiser", "BusinessCustomer"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Every subscriber-eligible role can browse plans and manage their own subscription.
            foreach (var permission in new[]
                     {
                         "subscription.plans.read", "subscription.read.own", "subscription.create",
                         "subscription.renew.own", "subscription.cancel.own"
                     })
            {
                foreach (var role in SubscriberRoles)
                {
                    migrationBuilder.InsertData(
                        table: "RolePermissions",
                        columns: new[] { "Role", "PermissionCode" },
                        values: new object[,] { { role, permission } });
                }
            }

            // Admin: manages the plan catalog and has global subscription oversight (e.g. the expiration sweep).
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "subscription.plans.manage" },
                    { "Admin", "subscription.manage" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in new[]
                     {
                         "subscription.plans.read", "subscription.read.own", "subscription.create",
                         "subscription.renew.own", "subscription.cancel.own"
                     })
            {
                foreach (var role in SubscriberRoles)
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
                keyValues: new object[] { "Admin", "subscription.plans.manage" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "subscription.manage" });
        }
    }
}
