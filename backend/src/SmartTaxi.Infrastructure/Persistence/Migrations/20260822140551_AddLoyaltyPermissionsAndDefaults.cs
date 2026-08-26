using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoyaltyPermissionsAndDefaults : Migration
    {
        private static readonly string[] EligibleActorRoles = ["Customer", "Driver"];

        private static readonly string[] SelfServicePermissions =
        [
            "loyalty.account.read.own", "loyalty.rewards.read", "loyalty.redemption.create.own", "loyalty.challenges.read"
        ];

        private static readonly DateTime SeedTimestamp = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);

        // Fixed ids so Up/Down stay symmetric across re-runs — same convention as any other data-seed migration.
        private static readonly Guid BronzeThresholdId = Guid.Parse("00000000-0000-0000-0000-000000000b01");
        private static readonly Guid SilverThresholdId = Guid.Parse("00000000-0000-0000-0000-000000000b02");
        private static readonly Guid GoldThresholdId = Guid.Parse("00000000-0000-0000-0000-000000000b03");
        private static readonly Guid PlatinumThresholdId = Guid.Parse("00000000-0000-0000-0000-000000000b04");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Self-service: only the two eligible actor roles (Customer, Driver) — see the audit's explicit scope decision.
            foreach (var permission in SelfServicePermissions)
            {
                foreach (var role in EligibleActorRoles)
                {
                    migrationBuilder.InsertData(
                        table: "RolePermissions",
                        columns: new[] { "Role", "PermissionCode" },
                        values: new object[,] { { role, permission } });
                }
            }

            // Admin: manages earning rules, tier thresholds, the reward/challenge catalog, and manual point adjustments.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "loyalty.rules.manage" },
                    { "Admin", "loyalty.catalog.manage" },
                    { "Admin", "loyalty.adjustments.manage" }
                });

            // Sensible out-of-the-box tier catalog — admin-editable via UpdateTierThreshold, never hard-coded in a handler.
            migrationBuilder.InsertData(
                table: "LoyaltyTierThresholds",
                columns: new[] { "Id", "Tier", "MinimumStatusPoints", "CreatedAtUtc", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { BronzeThresholdId, "Bronze", 0, SeedTimestamp, SeedTimestamp },
                    { SilverThresholdId, "Silver", 500, SeedTimestamp, SeedTimestamp },
                    { GoldThresholdId, "Gold", 2000, SeedTimestamp, SeedTimestamp },
                    { PlatinumThresholdId, "Platinum", 5000, SeedTimestamp, SeedTimestamp }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var permission in SelfServicePermissions)
            {
                foreach (var role in EligibleActorRoles)
                {
                    migrationBuilder.DeleteData(
                        table: "RolePermissions",
                        keyColumns: new[] { "Role", "PermissionCode" },
                        keyValues: new object[] { role, permission });
                }
            }

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "loyalty.rules.manage" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "loyalty.catalog.manage" });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[] { "Admin", "loyalty.adjustments.manage" });

            migrationBuilder.DeleteData(table: "LoyaltyTierThresholds", keyColumn: "Id", keyValue: BronzeThresholdId);
            migrationBuilder.DeleteData(table: "LoyaltyTierThresholds", keyColumn: "Id", keyValue: SilverThresholdId);
            migrationBuilder.DeleteData(table: "LoyaltyTierThresholds", keyColumn: "Id", keyValue: GoldThresholdId);
            migrationBuilder.DeleteData(table: "LoyaltyTierThresholds", keyColumn: "Id", keyValue: PlatinumThresholdId);
        }
    }
}
