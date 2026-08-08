using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRidePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Customer: requests/manages their own Rides, always the one who picks the Driver.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Customer", "rides.create" },
                    { "Customer", "rides.read.own" },
                    { "Customer", "rides.search-drivers" },
                    { "Customer", "rides.select-driver" },
                    { "Customer", "rides.cancel.own" },
                    { "Customer", "rides.rate" },
                    { "Customer", "rides.sos" },
                    { "Customer", "rides.shared.manage" }
                });

            // Driver: responds to selection and drives the lifecycle from acceptance to completion.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Driver", "rides.read.own" },
                    { "Driver", "rides.accept" },
                    { "Driver", "rides.reject" },
                    { "Driver", "rides.update-location" },
                    { "Driver", "rides.start" },
                    { "Driver", "rides.complete" },
                    { "Driver", "rides.cancel.own" },
                    { "Driver", "rides.rate" },
                    { "Driver", "rides.sos" },
                    { "Driver", "rides.shared.manage" }
                });

            // Admin: the master prompt describes separate PlatformAdmin/OperationsManager/
            // SupportAgent/SecurityOfficer roles, but UserRole currently has a single Admin
            // tier — all platform-level Ride permissions are granted to it, documented here
            // as the known mismatch rather than inventing roles the rest of the codebase
            // (Identity's UserRole enum) doesn't define.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "rides.cancel.admin" },
                    { "Admin", "rides.monitor" },
                    { "Admin", "rides.dispute" },
                    { "Admin", "rides.pricing.manage" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[,]
                {
                    { "Customer", "rides.create" },
                    { "Customer", "rides.read.own" },
                    { "Customer", "rides.search-drivers" },
                    { "Customer", "rides.select-driver" },
                    { "Customer", "rides.cancel.own" },
                    { "Customer", "rides.rate" },
                    { "Customer", "rides.sos" },
                    { "Customer", "rides.shared.manage" },
                    { "Driver", "rides.read.own" },
                    { "Driver", "rides.accept" },
                    { "Driver", "rides.reject" },
                    { "Driver", "rides.update-location" },
                    { "Driver", "rides.start" },
                    { "Driver", "rides.complete" },
                    { "Driver", "rides.cancel.own" },
                    { "Driver", "rides.rate" },
                    { "Driver", "rides.sos" },
                    { "Driver", "rides.shared.manage" },
                    { "Admin", "rides.cancel.admin" },
                    { "Admin", "rides.monitor" },
                    { "Admin", "rides.dispute" },
                    { "Admin", "rides.pricing.manage" }
                });
        }
    }
}
