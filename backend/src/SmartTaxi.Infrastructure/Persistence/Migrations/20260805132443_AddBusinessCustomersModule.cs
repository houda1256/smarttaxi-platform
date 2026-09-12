using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCustomersModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessCustomerEmployees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessCustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RideBudgetPerMonth = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    AllowedVehicleCategories = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AllowedScheduleStart = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    AllowedScheduleEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    AllowedZones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PerRideLimit = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCustomerEmployees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BusinessCustomers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BillingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContactPersonName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPersonEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    ContactPersonPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PaymentTerms = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CurrentCreditUsage = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCustomers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCustomerEmployees_BusinessCustomerId",
                table: "BusinessCustomerEmployees",
                column: "BusinessCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCustomerEmployees_UserId",
                table: "BusinessCustomerEmployees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCustomers_Status",
                table: "BusinessCustomers",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCustomers_TaxIdentifier",
                table: "BusinessCustomers",
                column: "TaxIdentifier",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessCustomerEmployees");

            migrationBuilder.DropTable(
                name: "BusinessCustomers");
        }
    }
}
