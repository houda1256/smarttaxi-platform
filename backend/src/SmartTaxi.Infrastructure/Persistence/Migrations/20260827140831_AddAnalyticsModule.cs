using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ScheduledReportDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextRunAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReportDefinitions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_CreatedAt",
                table: "Vehicles",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAtUtc",
                table: "Users",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportTickets_CreatedAtUtc",
                table: "SupportTickets",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SupportIncidents_CreatedAtUtc",
                table: "SupportIncidents",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoadsideAssistanceRequests_RequestedAtUtc",
                table: "RoadsideAssistanceRequests",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRequests_RequestedAtUtc",
                table: "MaintenanceRequests",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_EntryType_CreatedAt",
                table: "FinancialLedgerEntries",
                columns: new[] { "EntryType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AdCampaigns_CreatedAtUtc",
                table: "AdCampaigns",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReportDefinitions_IsActive_NextRunAtUtc",
                table: "ScheduledReportDefinitions",
                columns: new[] { "IsActive", "NextRunAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledReportDefinitions_RecipientUserId",
                table: "ScheduledReportDefinitions",
                column: "RecipientUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledReportDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_CreatedAt",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedAtUtc",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SupportTickets_CreatedAtUtc",
                table: "SupportTickets");

            migrationBuilder.DropIndex(
                name: "IX_SupportIncidents_CreatedAtUtc",
                table: "SupportIncidents");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_CreatedAt",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_RoadsideAssistanceRequests_RequestedAtUtc",
                table: "RoadsideAssistanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceRequests_RequestedAtUtc",
                table: "MaintenanceRequests");

            migrationBuilder.DropIndex(
                name: "IX_FinancialLedgerEntries_EntryType_CreatedAt",
                table: "FinancialLedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_AdCampaigns_CreatedAtUtc",
                table: "AdCampaigns");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Users");
        }
    }
}
