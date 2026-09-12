using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadsideAssistancePermissions : Migration
    {
        private static readonly string[] RoadsideAssistancePartnerPermissions =
        [
            "roadside.partner-profile.manage.own", "roadside.jobs.manage.own"
        ];

        private static readonly string[] TaxiOwnerPermissions =
        [
            "roadside.requests.create.own", "roadside.requests.read.own", "roadside.requests.manage.own"
        ];

        private static readonly string[] DriverPermissions =
        [
            "roadside.requests.create.own", "roadside.requests.read.own", "roadside.requests.manage.own"
        ];

        private static readonly string[] AdminPermissions = ["roadside.read.all", "roadside.manage.all"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in RoadsideAssistancePartnerPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" },
                    values: new object[,] { { "RoadsideAssistancePartner", permission } });
            }

            foreach (var permission in TaxiOwnerPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "TaxiOwner", permission } });
            }

            foreach (var permission in DriverPermissions)
            {
                migrationBuilder.InsertData(
                    table: "RolePermissions", columns: new[] { "Role", "PermissionCode" }, values: new object[,] { { "Driver", permission } });
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
            foreach (var permission in RoadsideAssistancePartnerPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" },
                    keyValues: new object[] { "RoadsideAssistancePartner", permission });
            }

            foreach (var permission in TaxiOwnerPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "TaxiOwner", permission });
            }

            foreach (var permission in DriverPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Driver", permission });
            }

            foreach (var permission in AdminPermissions)
            {
                migrationBuilder.DeleteData(
                    table: "RolePermissions", keyColumns: new[] { "Role", "PermissionCode" }, keyValues: new object[] { "Admin", permission });
            }
        }
    }
}
