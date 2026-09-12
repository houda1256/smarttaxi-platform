using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add2eProfessionalAccountsReferralsPreferencesDataRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PersonalDataRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResultReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalDataRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalAccountRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalAccountRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Referrals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferrerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefereeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCodeUsed = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Referrals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Language = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NotificationChannels = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ShareProfileWithPartners = table.Column<bool>(type: "boolean", nullable: false),
                    AllowMarketingCommunications = table.Column<bool>(type: "boolean", nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ReferralCode",
                table: "Users",
                column: "ReferralCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataRequests_Status",
                table: "PersonalDataRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalDataRequests_UserId",
                table: "PersonalDataRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalAccountRequests_Status",
                table: "ProfessionalAccountRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalAccountRequests_UserId",
                table: "ProfessionalAccountRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_RefereeUserId",
                table: "Referrals",
                column: "RefereeUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_ReferrerUserId",
                table: "Referrals",
                column: "ReferrerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Referrals_Status",
                table: "Referrals",
                column: "Status");

            // Admin gets the review/management permissions.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "professional.review" },
                    { "Admin", "referrals.manage" },
                    { "Admin", "data-requests.process" }
                });

            // Every role gets the baseline self-service permissions: applying
            // for a professional role, managing one's own preferences, and
            // submitting personal-data requests are available to any account.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Customer", "professional.register.own" },
                    { "Customer", "preferences.manage.own" },
                    { "Customer", "data-requests.submit.own" },
                    { "Driver", "professional.register.own" },
                    { "Driver", "preferences.manage.own" },
                    { "Driver", "data-requests.submit.own" },
                    { "Admin", "professional.register.own" },
                    { "Admin", "preferences.manage.own" },
                    { "Admin", "data-requests.submit.own" },
                    { "TaxiOwner", "professional.register.own" },
                    { "TaxiOwner", "preferences.manage.own" },
                    { "TaxiOwner", "data-requests.submit.own" },
                    { "GaragePartner", "professional.register.own" },
                    { "GaragePartner", "preferences.manage.own" },
                    { "GaragePartner", "data-requests.submit.own" },
                    { "RoadsideAssistancePartner", "professional.register.own" },
                    { "RoadsideAssistancePartner", "preferences.manage.own" },
                    { "RoadsideAssistancePartner", "data-requests.submit.own" },
                    { "Advertiser", "professional.register.own" },
                    { "Advertiser", "preferences.manage.own" },
                    { "Advertiser", "data-requests.submit.own" },
                    { "BusinessCustomer", "professional.register.own" },
                    { "BusinessCustomer", "preferences.manage.own" },
                    { "BusinessCustomer", "data-requests.submit.own" }
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
                    { "Admin", "professional.review" },
                    { "Admin", "referrals.manage" },
                    { "Admin", "data-requests.process" },
                    { "Customer", "professional.register.own" },
                    { "Customer", "preferences.manage.own" },
                    { "Customer", "data-requests.submit.own" },
                    { "Driver", "professional.register.own" },
                    { "Driver", "preferences.manage.own" },
                    { "Driver", "data-requests.submit.own" },
                    { "Admin", "professional.register.own" },
                    { "Admin", "preferences.manage.own" },
                    { "Admin", "data-requests.submit.own" },
                    { "TaxiOwner", "professional.register.own" },
                    { "TaxiOwner", "preferences.manage.own" },
                    { "TaxiOwner", "data-requests.submit.own" },
                    { "GaragePartner", "professional.register.own" },
                    { "GaragePartner", "preferences.manage.own" },
                    { "GaragePartner", "data-requests.submit.own" },
                    { "RoadsideAssistancePartner", "professional.register.own" },
                    { "RoadsideAssistancePartner", "preferences.manage.own" },
                    { "RoadsideAssistancePartner", "data-requests.submit.own" },
                    { "Advertiser", "professional.register.own" },
                    { "Advertiser", "preferences.manage.own" },
                    { "Advertiser", "data-requests.submit.own" },
                    { "BusinessCustomer", "professional.register.own" },
                    { "BusinessCustomer", "preferences.manage.own" },
                    { "BusinessCustomer", "data-requests.submit.own" }
                });

            migrationBuilder.DropTable(
                name: "PersonalDataRequests");

            migrationBuilder.DropTable(
                name: "ProfessionalAccountRequests");

            migrationBuilder.DropTable(
                name: "Referrals");

            migrationBuilder.DropTable(
                name: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_Users_ReferralCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "Users");
        }
    }
}
