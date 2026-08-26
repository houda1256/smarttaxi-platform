using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenancePermissions : Migration
    {
        private static readonly string[] GaragePartnerPermissions =
        [
            "maintenance.garage-profile.manage.own", "maintenance.jobs.manage.own"
        ];

        private static readonly string[] TaxiOwnerPermissions =
        [
            "maintenance.requests.create.own", "maintenance.requests.read.own", "maintenance.requests.manage.own",
            "maintenance.records.read.own"
        ];

        private static readonly string[] AdminPermissions = ["maintenance.read.all", "maintenance.manage.all"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in GaragePartnerPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "GaragePartner", permission } });
            }

            foreach (var permission in TaxiOwnerPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "TaxiOwner", permission } });
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
            foreach (var permission in GaragePartnerPermissions)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "GaragePartner", permission });
            }

            foreach (var permission in TaxiOwnerPermissions)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "TaxiOwner", permission });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.DeleteData(table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }
        }
    }
}
