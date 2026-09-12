using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GarageProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportedVehicleCategories = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AvailableServices = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GarageProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GarageUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterventionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MileageAtCompletion = table.Column<int>(type: "integer", nullable: true),
                    FinalCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    NextRecommendedServiceDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WarrantyInfo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRecords_FinalCost_NonNegative", "\"FinalCost\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    GarageUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GarageRejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    QuoteSubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    QuoteAcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VehicleReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WorkStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinalCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRequests", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceRequests_EstimatedCost_NonNegative", "\"EstimatedCost\" IS NULL OR \"EstimatedCost\" >= 0");
                    table.CheckConstraint("CK_MaintenanceRequests_FinalCost_NonNegative", "\"FinalCost\" IS NULL OR \"FinalCost\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "MaintenanceRecordLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsPart = table.Column<bool>(type: "boolean", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    MaintenanceRecordId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecordLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecordLines_MaintenanceRecords_MaintenanceRecord~",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "MaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GarageProfiles_UserId",
                table: "GarageProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecordLines_MaintenanceRecordId",
                table: "MaintenanceRecordLines",
                column: "MaintenanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_MaintenanceRequestId",
                table: "MaintenanceRecords",
                column: "MaintenanceRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_NextRecommendedServiceDate",
                table: "MaintenanceRecords",
                column: "NextRecommendedServiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_VehicleId",
                table: "MaintenanceRecords",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_GarageUserId",
                table: "MaintenanceRequests",
                column: "GarageUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_OwnerUserId",
                table: "MaintenanceRequests",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_Status",
                table: "MaintenanceRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_VehicleId_Active",
                table: "MaintenanceRequests",
                column: "VehicleId",
                unique: true,
                filter: "\"Status\" NOT IN ('Completed', 'Cancelled', 'Rejected', 'QuoteRejected')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GarageProfiles");

            migrationBuilder.DropTable(
                name: "MaintenanceRecordLines");

            migrationBuilder.DropTable(
                name: "MaintenanceRequests");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");
        }
    }
}
