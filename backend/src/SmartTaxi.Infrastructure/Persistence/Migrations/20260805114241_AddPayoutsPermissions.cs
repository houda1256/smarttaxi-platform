using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Driver, TaxiOwner, GaragePartner, RoadsideAssistancePartner: the four beneficiary types a
            // Payout may be issued to (Payout.Request's own invariant) — each may request and read their own.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Driver", "finance.payouts.request.own" },
                    { "Driver", "finance.payouts.read.own" },
                    { "TaxiOwner", "finance.payouts.request.own" },
                    { "TaxiOwner", "finance.payouts.read.own" },
                    { "GaragePartner", "finance.payouts.request.own" },
                    { "GaragePartner", "finance.payouts.read.own" },
                    { "RoadsideAssistancePartner", "finance.payouts.request.own" },
                    { "RoadsideAssistancePartner", "finance.payouts.read.own" }
                });

            // Admin: approves/rejects/processes/completes/fails/cancels any Payout — full finance oversight.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "finance.payouts.manage" }
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
                    { "Driver", "finance.payouts.request.own" },
                    { "Driver", "finance.payouts.read.own" },
                    { "TaxiOwner", "finance.payouts.request.own" },
                    { "TaxiOwner", "finance.payouts.read.own" },
                    { "GaragePartner", "finance.payouts.request.own" },
                    { "GaragePartner", "finance.payouts.read.own" },
                    { "RoadsideAssistancePartner", "finance.payouts.request.own" },
                    { "RoadsideAssistancePartner", "finance.payouts.read.own" },
                    { "Admin", "finance.payouts.manage" }
                });
        }
    }
}
