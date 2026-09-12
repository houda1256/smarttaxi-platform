using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TaxiOwner: self-service management of everything they own (profile, fleets,
            // vehicles, vehicle documents, assignments, contracts, expenses, alerts, usage history).
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "TaxiOwner", "fleet.owner-profile.manage.own" },
                    { "TaxiOwner", "fleet.manage.own" },
                    { "TaxiOwner", "fleet.vehicles.manage.own" },
                    { "TaxiOwner", "fleet.vehicle-documents.manage.own" },
                    { "TaxiOwner", "fleet.assignments.manage.own" },
                    { "TaxiOwner", "fleet.contracts.manage.own" },
                    { "TaxiOwner", "fleet.expenses.manage.own" },
                    { "TaxiOwner", "fleet.alerts.manage.own" },
                    { "TaxiOwner", "fleet.usage.read.own" }
                });

            // Driver: manages their own driver profile/availability and reads their own contracts.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Driver", "fleet.drivers.manage.own" },
                    { "Driver", "fleet.contracts.read.own" }
                });

            // Admin: platform-level verification gates — an owner can never approve/reject
            // their own vehicles, documents, or drivers, regardless of any other permission.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "fleet.vehicles.review" },
                    { "Admin", "fleet.vehicle-documents.review" },
                    { "Admin", "fleet.vehicle-documents.read.all" },
                    { "Admin", "fleet.drivers.review" }
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
                    { "TaxiOwner", "fleet.owner-profile.manage.own" },
                    { "TaxiOwner", "fleet.manage.own" },
                    { "TaxiOwner", "fleet.vehicles.manage.own" },
                    { "TaxiOwner", "fleet.vehicle-documents.manage.own" },
                    { "TaxiOwner", "fleet.assignments.manage.own" },
                    { "TaxiOwner", "fleet.contracts.manage.own" },
                    { "TaxiOwner", "fleet.expenses.manage.own" },
                    { "TaxiOwner", "fleet.alerts.manage.own" },
                    { "TaxiOwner", "fleet.usage.read.own" },
                    { "Driver", "fleet.drivers.manage.own" },
                    { "Driver", "fleet.contracts.read.own" },
                    { "Admin", "fleet.vehicles.review" },
                    { "Admin", "fleet.vehicle-documents.review" },
                    { "Admin", "fleet.vehicle-documents.read.all" },
                    { "Admin", "fleet.drivers.review" }
                });
        }
    }
}
