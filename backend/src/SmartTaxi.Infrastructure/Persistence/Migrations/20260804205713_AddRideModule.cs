using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRideModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "LastKnownLatitude",
                table: "DriverProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LastKnownLongitude",
                table: "DriverProfiles",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLocationRecordedAt",
                table: "DriverProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DriverReservationHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverReservationHolds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideComplaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComplainantUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcernedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideComplaints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReadOnlyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideDriverRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceToPickupKm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    EstimatedArrivalMinutes = table.Column<int>(type: "integer", nullable: false),
                    RecommendationScore = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    RecommendationReasons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideDriverRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideFareProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RoundNumber = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideFareProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideLocationPoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Speed = table.Column<double>(type: "double precision", nullable: true),
                    Heading = table.Column<double>(type: "double precision", nullable: true),
                    Accuracy = table.Column<double>(type: "double precision", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideLocationPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsReported = table.Column<bool>(type: "boolean", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsReported = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SelectedDriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RideType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PickupAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PickupLatitude = table.Column<double>(type: "double precision", nullable: false),
                    PickupLongitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DestinationLatitude = table.Column<double>(type: "double precision", nullable: false),
                    DestinationLongitude = table.Column<double>(type: "double precision", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PassengerCount = table.Column<int>(type: "integer", nullable: false),
                    LuggageCount = table.Column<int>(type: "integer", nullable: false),
                    NeedsAirConditioning = table.Column<bool>(type: "boolean", nullable: false),
                    NeedsAccessibleVehicle = table.Column<bool>(type: "boolean", nullable: false),
                    HasChildSeatRequest = table.Column<bool>(type: "boolean", nullable: false),
                    HasPet = table.Column<bool>(type: "boolean", nullable: false),
                    PreferredVehicleCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PreferredPaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SpecialInstructions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EstimatedDistanceKm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    ActualDistanceKm = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    ActualDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    EstimatedFare = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    FinalFare = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    NegotiatedFinalFare = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    LastKnownLatitude = table.Column<double>(type: "double precision", nullable: true),
                    LastKnownLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LastLocationRecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DriverArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideSafetyEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggeredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideSafetyEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideShareTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideShareTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideStatusHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedRideMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRideMatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SharedRideParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SharedRideMatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RideId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedRideParticipants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverReservationHolds_DriverId",
                table: "DriverReservationHolds",
                column: "DriverId",
                unique: true,
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_DriverReservationHolds_ExpiresAt",
                table: "DriverReservationHolds",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_DriverReservationHolds_RideId",
                table: "DriverReservationHolds",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideComplaints_RideId",
                table: "RideComplaints",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideComplaints_Status",
                table: "RideComplaints",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RideConversations_RideId",
                table: "RideConversations",
                column: "RideId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RideDriverRecommendations_RideId",
                table: "RideDriverRecommendations",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideFareProposals_RideId",
                table: "RideFareProposals",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideFareProposals_RideId_RoundNumber",
                table: "RideFareProposals",
                columns: new[] { "RideId", "RoundNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_RideLocationPoints_RecordedAt",
                table: "RideLocationPoints",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RideLocationPoints_RideId_RecordedAt",
                table: "RideLocationPoints",
                columns: new[] { "RideId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RideMessages_ConversationId_SentAt",
                table: "RideMessages",
                columns: new[] { "ConversationId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RideRatings_ReviewedUserId",
                table: "RideRatings",
                column: "ReviewedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRatings_RideId_ReviewerId",
                table: "RideRatings",
                columns: new[] { "RideId", "ReviewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_CustomerId",
                table: "Rides",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_RequestedAt",
                table: "Rides",
                column: "RequestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_RideNumber",
                table: "Rides",
                column: "RideNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Rides_RideType_Status",
                table: "Rides",
                columns: new[] { "RideType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_ScheduledAt",
                table: "Rides",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_SelectedDriverId",
                table: "Rides",
                column: "SelectedDriverId");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_SelectedDriverId_Status",
                table: "Rides",
                columns: new[] { "SelectedDriverId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Rides_Status",
                table: "Rides",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Rides_VehicleId",
                table: "Rides",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_RideSafetyEvents_RideId",
                table: "RideSafetyEvents",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideSafetyEvents_Status",
                table: "RideSafetyEvents",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RideShareTokens_RideId",
                table: "RideShareTokens",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_RideShareTokens_TokenHash",
                table: "RideShareTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RideStatusHistories_RideId",
                table: "RideStatusHistories",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRideMatches_ExpiresAt",
                table: "SharedRideMatches",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRideMatches_Status",
                table: "SharedRideMatches",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRideParticipants_RideId",
                table: "SharedRideParticipants",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRideParticipants_SharedRideMatchId",
                table: "SharedRideParticipants",
                column: "SharedRideMatchId");

            migrationBuilder.CreateIndex(
                name: "IX_SharedRideParticipants_SharedRideMatchId_RideId",
                table: "SharedRideParticipants",
                columns: new[] { "SharedRideMatchId", "RideId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverReservationHolds");

            migrationBuilder.DropTable(
                name: "RideComplaints");

            migrationBuilder.DropTable(
                name: "RideConversations");

            migrationBuilder.DropTable(
                name: "RideDriverRecommendations");

            migrationBuilder.DropTable(
                name: "RideFareProposals");

            migrationBuilder.DropTable(
                name: "RideLocationPoints");

            migrationBuilder.DropTable(
                name: "RideMessages");

            migrationBuilder.DropTable(
                name: "RideRatings");

            migrationBuilder.DropTable(
                name: "Rides");

            migrationBuilder.DropTable(
                name: "RideSafetyEvents");

            migrationBuilder.DropTable(
                name: "RideShareTokens");

            migrationBuilder.DropTable(
                name: "RideStatusHistories");

            migrationBuilder.DropTable(
                name: "SharedRideMatches");

            migrationBuilder.DropTable(
                name: "SharedRideParticipants");

            migrationBuilder.DropColumn(
                name: "LastKnownLatitude",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "LastKnownLongitude",
                table: "DriverProfiles");

            migrationBuilder.DropColumn(
                name: "LastLocationRecordedAt",
                table: "DriverProfiles");
        }
    }
}
