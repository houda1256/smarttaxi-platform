using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDisputesPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Any actor who can be a party to a Payment/Invoice/Payout may raise a dispute over it.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Customer", "finance.disputes.open.own" },
                    { "Driver", "finance.disputes.open.own" },
                    { "TaxiOwner", "finance.disputes.open.own" },
                    { "GaragePartner", "finance.disputes.open.own" },
                    { "RoadsideAssistancePartner", "finance.disputes.open.own" }
                });

            // Admin: reviews, assigns, resolves, rejects, and escalates any dispute.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "finance.disputes.manage" }
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
                    { "Customer", "finance.disputes.open.own" },
                    { "Driver", "finance.disputes.open.own" },
                    { "TaxiOwner", "finance.disputes.open.own" },
                    { "GaragePartner", "finance.disputes.open.own" },
                    { "RoadsideAssistancePartner", "finance.disputes.open.own" },
                    { "Admin", "finance.disputes.manage" }
                });
        }
    }
}
