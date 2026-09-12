using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyModule7AuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "LoyaltyRedemptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RemainingAmount",
                table: "LoyaltyPointLedgerEntries",
                type: "integer",
                nullable: true);

            // Backfill: pre-existing redemption rows have no client-supplied idempotency key, so give
            // each its own Id as a placeholder (unique by construction) before the unique index below is
            // created — a genuine client retry of a NEW redemption still gets real request-level
            // idempotency going forward; this only avoids breaking history that predates this fix.
            migrationBuilder.Sql(
                "UPDATE \"LoyaltyRedemptions\" SET \"IdempotencyKey\" = \"Id\"::text WHERE \"IdempotencyKey\" = '';");

            // Backfill: one-time assumption that no pre-existing Earn/RewardPoints lot has already been
            // partially consumed under the old account-balance-only expiration model — RemainingAmount
            // starts equal to each lot's original Points. This is an operational-state initialization, not
            // a rewrite of any row's immutable audit fields (Points/BalanceAfter/Reason are untouched).
            migrationBuilder.Sql(
                "UPDATE \"LoyaltyPointLedgerEntries\" SET \"RemainingAmount\" = \"Points\" " +
                "WHERE \"EntryType\" = 'Earn' AND \"PointType\" = 'RewardPoints';");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyRedemptions_UserId_IdempotencyKey",
                table: "LoyaltyRedemptions",
                columns: new[] { "UserId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoyaltyPointLedgerEntries_RemainingAmount_NonNegative",
                table: "LoyaltyPointLedgerEntries",
                sql: "\"RemainingAmount\" IS NULL OR \"RemainingAmount\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LoyaltyRedemptions_UserId_IdempotencyKey",
                table: "LoyaltyRedemptions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoyaltyPointLedgerEntries_RemainingAmount_NonNegative",
                table: "LoyaltyPointLedgerEntries");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "LoyaltyRedemptions");

            migrationBuilder.DropColumn(
                name: "RemainingAmount",
                table: "LoyaltyPointLedgerEntries");
        }
    }
}
