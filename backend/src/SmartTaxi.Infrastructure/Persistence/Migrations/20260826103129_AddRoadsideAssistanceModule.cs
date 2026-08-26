using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRoadsideAssistanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoadsideAssistanceRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: true),
                    ServiceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Urgency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SelectedPartnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    FinalCost = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PartnerOnTheWayAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PartnerArrivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DisputedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    EscalatedMaintenanceRequestId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadsideAssistanceRequests", x => x.Id);
                    table.CheckConstraint("CK_RoadsideAssistanceRequests_EstimatedCost_NonNegative", "\"EstimatedCost\" IS NULL OR \"EstimatedCost\" >= 0");
                    table.CheckConstraint("CK_RoadsideAssistanceRequests_FinalCost_NonNegative", "\"FinalCost\" IS NULL OR \"FinalCost\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "RoadsidePartnerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SupportedServiceTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SupportedVehicleCategories = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadsidePartnerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoadsidePartnerSelectionHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoadsideAssistanceRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    CycleNumber = table.Column<int>(type: "integer", nullable: false),
                    SelectedPartnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Response = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadsidePartnerSelectionHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoadsideAssistanceRequests_RequesterUserId",
                table: "RoadsideAssistanceRequests",
                column: "RequesterUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadsideAssistanceRequests_SelectedPartnerUserId",
                table: "RoadsideAssistanceRequests",
                column: "SelectedPartnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadsideAssistanceRequests_Status",
                table: "RoadsideAssistanceRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RoadsideAssistanceRequests_VehicleId_Active",
                table: "RoadsideAssistanceRequests",
                column: "VehicleId",
                unique: true,
                filter: "\"Status\" NOT IN ('Completed', 'Cancelled', 'Expired', 'Disputed')");

            migrationBuilder.CreateIndex(
                name: "IX_RoadsidePartnerProfiles_UserId",
                table: "RoadsidePartnerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadsidePartnerSelectionHistories_RoadsideAssistanceReques~1",
                table: "RoadsidePartnerSelectionHistories",
                columns: new[] { "RoadsideAssistanceRequestId", "CycleNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadsidePartnerSelectionHistories_RoadsideAssistanceRequest~",
                table: "RoadsidePartnerSelectionHistories",
                column: "RoadsideAssistanceRequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoadsideAssistanceRequests");

            migrationBuilder.DropTable(
                name: "RoadsidePartnerProfiles");

            migrationBuilder.DropTable(
                name: "RoadsidePartnerSelectionHistories");
        }
    }
}
