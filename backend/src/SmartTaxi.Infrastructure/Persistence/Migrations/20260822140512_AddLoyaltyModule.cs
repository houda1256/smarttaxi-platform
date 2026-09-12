using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoyaltyAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CurrentRewardPoints = table.Column<int>(type: "integer", nullable: false),
                    CurrentStatusPoints = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyChallengeProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentValue = table.Column<int>(type: "integer", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RewardedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyChallengeProgress", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CriteriaType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetValue = table.Column<int>(type: "integer", nullable: false),
                    RewardPoints = table.Column<int>(type: "integer", nullable: false),
                    EligibleRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyEarningRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RewardPointsPerCurrencyUnit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    StatusPointsPerCurrencyUnit = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    MinPoints = table.Column<int>(type: "integer", nullable: true),
                    MaxPoints = table.Column<int>(type: "integer", nullable: true),
                    SubscriptionMultiplierAllowed = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyEarningRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarningRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    RewardId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpirationAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoyaltyAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardId = table.Column<Guid>(type: "uuid", nullable: false),
                    PointsSpent = table.Column<int>(type: "integer", nullable: false),
                    RedeemedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRedemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyReferralRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefereeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerRewardPoints = table.Column<int>(type: "integer", nullable: false),
                    RefereeRewardPoints = table.Column<int>(type: "integer", nullable: false),
                    RuleCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GrantedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyReferralRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyRewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CostInRewardPoints = table.Column<int>(type: "integer", nullable: false),
                    RewardType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TargetRoles = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageLimit = table.Column<int>(type: "integer", nullable: true),
                    RedeemedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTierThresholds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinimumStatusPoints = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTierThresholds", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyAccounts_UserId",
                table: "LoyaltyAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallengeProgress_UserId_ChallengeId",
                table: "LoyaltyChallengeProgress",
                columns: new[] { "UserId", "ChallengeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallenges_Code",
                table: "LoyaltyChallenges",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyChallenges_IsActive",
                table: "LoyaltyChallenges",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyEarningRules_ActorRole_SourceType_IsActive",
                table: "LoyaltyEarningRules",
                columns: new[] { "ActorRole", "SourceType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyEarningRules_Code",
                table: "LoyaltyEarningRules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_ExpirationAtUtc",
                table: "LoyaltyPointLedgerEntries",
                column: "ExpirationAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_IdempotencyKey",
                table: "LoyaltyPointLedgerEntries",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointLedgerEntries_UserId_CreatedAtUtc",
                table: "LoyaltyPointLedgerEntries",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_UserId_RedeemedAtUtc",
                table: "LoyaltyRedemptions",
                columns: new[] { "UserId", "RedeemedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyReferralRewards_ReferralId",
                table: "LoyaltyReferralRewards",
                column: "ReferralId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyReferralRewards_ReferrerUserId",
                table: "LoyaltyReferralRewards",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_Code",
                table: "LoyaltyRewards",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRewards_IsActive",
                table: "LoyaltyRewards",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTierThresholds_Tier",
                table: "LoyaltyTierThresholds",
                column: "Tier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoyaltyAccounts");

            migrationBuilder.DropTable(
                name: "LoyaltyChallengeProgress");

            migrationBuilder.DropTable(
                name: "LoyaltyChallenges");

            migrationBuilder.DropTable(
                name: "LoyaltyEarningRules");

            migrationBuilder.DropTable(
                name: "LoyaltyPointLedgerEntries");

            migrationBuilder.DropTable(
                name: "LoyaltyRedemptions");

            migrationBuilder.DropTable(
                name: "LoyaltyReferralRewards");

            migrationBuilder.DropTable(
                name: "LoyaltyRewards");

            migrationBuilder.DropTable(
                name: "LoyaltyTierThresholds");
        }
    }
}
