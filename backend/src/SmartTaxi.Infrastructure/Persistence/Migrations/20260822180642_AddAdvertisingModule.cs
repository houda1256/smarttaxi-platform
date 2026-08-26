using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvertisingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvertiserProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdvertiserUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Objective = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PlacementId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PricingModel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PriceRate = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    BudgetLimit = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    ConsumedBudget = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    DailyBudgetLimit = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: true),
                    DailyConsumedBudget = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    DailyConsumedDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TargetCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TargetVehicleCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetDaysOfWeek = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetStartHour = table.Column<int>(type: "integer", nullable: true),
                    TargetEndHour = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SettledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdCampaigns", x => x.Id);
                    table.CheckConstraint("CK_AdCampaigns_BudgetLimit_NonNegative", "\"BudgetLimit\" >= 0");
                    table.CheckConstraint("CK_AdCampaigns_ConsumedBudget_NonNegative", "\"ConsumedBudget\" >= 0");
                    table.CheckConstraint("CK_AdCampaigns_EndAfterStart", "\"EndAtUtc\" > \"StartAtUtc\"");
                });

            migrationBuilder.CreateTable(
                name: "AdvertiserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertiserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingCampaignReviewHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReviewerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PreviousStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    NewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingCampaignReviewHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingClicks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImpressionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperationalCost = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingClicks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingCreatives",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ReplacesCreativeId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingCreatives", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingImpressions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlacementId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperationalCost = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingImpressions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AdvertisingPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SupportedMediaTypes = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvertisingPlacements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_AdvertiserUserId",
                table: "AdCampaigns",
                column: "AdvertiserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_Code",
                table: "AdCampaigns",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_Status",
                table: "AdCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_Status_EndAtUtc",
                table: "AdCampaigns",
                columns: new[] { "Status", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_Status_StartAtUtc",
                table: "AdCampaigns",
                columns: new[] { "Status", "StartAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertiserProfiles_UserId",
                table: "AdvertiserProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingCampaignReviewHistory_CampaignId_CreatedAtUtc",
                table: "AdvertisingCampaignReviewHistory",
                columns: new[] { "CampaignId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingClicks_CampaignId_OccurredAtUtc",
                table: "AdvertisingClicks",
                columns: new[] { "CampaignId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingClicks_IdempotencyKey",
                table: "AdvertisingClicks",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingCreatives_CampaignId",
                table: "AdvertisingCreatives",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingImpressions_CampaignId_OccurredAtUtc",
                table: "AdvertisingImpressions",
                columns: new[] { "CampaignId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingImpressions_IdempotencyKey",
                table: "AdvertisingImpressions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingPlacements_Code",
                table: "AdvertisingPlacements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvertisingPlacements_IsActive",
                table: "AdvertisingPlacements",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdCampaigns");

            migrationBuilder.DropTable(
                name: "AdvertiserProfiles");

            migrationBuilder.DropTable(
                name: "AdvertisingCampaignReviewHistory");

            migrationBuilder.DropTable(
                name: "AdvertisingClicks");

            migrationBuilder.DropTable(
                name: "AdvertisingCreatives");

            migrationBuilder.DropTable(
                name: "AdvertisingImpressions");

            migrationBuilder.DropTable(
                name: "AdvertisingPlacements");
        }
    }
}
