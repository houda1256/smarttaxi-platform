using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportPermissions : Migration
    {
        /// <summary>Every non-admin role can raise and manage its own support tickets.</summary>
        private static readonly string[] OwnTicketPermissions =
        [
            "support.tickets.create.own", "support.tickets.read.own", "support.tickets.manage.own"
        ];

        private static readonly string[] SelfServiceRoles =
        [
            "Customer", "Driver", "TaxiOwner", "GaragePartner", "RoadsideAssistancePartner", "Advertiser", "BusinessCustomer"
        ];

        /// <summary>Admin-only — tickets across all requesters, plus the fully internal incidents surface.</summary>
        private static readonly string[] AdminPermissions =
        [
            "support.tickets.read.all", "support.tickets.manage.all", "support.incidents.read.all", "support.incidents.manage.all"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var role in SelfServiceRoles)
            {
                foreach (var permission in OwnTicketPermissions)
                {
                    migrationBuilder.InsertData(
                        table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { role, permission } });
                }
            }

            foreach (var permission in OwnTicketPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Admin", permission } });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Admin", permission } });
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var role in SelfServiceRoles)
            {
                foreach (var permission in OwnTicketPermissions)
                {
                    migrationBuilder.DeleteData(
                        table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { role, permission });
                }
            }

            foreach (var permission in OwnTicketPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }
        }
    }
}
