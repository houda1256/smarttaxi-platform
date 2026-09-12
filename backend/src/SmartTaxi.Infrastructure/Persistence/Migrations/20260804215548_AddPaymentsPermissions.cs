using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Customer: initiates and confirms the payment for their own completed Ride.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Customer", "payments.create" },
                    { "Customer", "payments.read.own" },
                    { "Customer", "payments.confirm" },
                    { "Customer", "payments.cancel" }
                });

            // Driver: same self-service actions (e.g. confirming a cash collection).
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Driver", "payments.create" },
                    { "Driver", "payments.read.own" },
                    { "Driver", "payments.confirm" },
                    { "Driver", "payments.cancel" }
                });

            // TaxiOwner: reads their own revenue history only — never confirms/cancels a Payment they aren't a party to.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "TaxiOwner", "payments.read.own" }
                });

            // Admin: platform-level financial oversight — refunds, reports, and full monitoring.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "payments.refund" },
                    { "Admin", "payments.report" },
                    { "Admin", "payments.manage" }
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
                    { "Customer", "payments.create" },
                    { "Customer", "payments.read.own" },
                    { "Customer", "payments.confirm" },
                    { "Customer", "payments.cancel" },
                    { "Driver", "payments.create" },
                    { "Driver", "payments.read.own" },
                    { "Driver", "payments.confirm" },
                    { "Driver", "payments.cancel" },
                    { "TaxiOwner", "payments.read.own" },
                    { "Admin", "payments.refund" },
                    { "Admin", "payments.report" },
                    { "Admin", "payments.manage" }
                });
        }
    }
}
