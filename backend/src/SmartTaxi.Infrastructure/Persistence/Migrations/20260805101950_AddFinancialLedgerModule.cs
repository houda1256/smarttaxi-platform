using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialLedgerModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TaxRateApplied",
                table: "Invoices",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxRuleId",
                table: "Invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRuleName",
                table: "Invoices",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    OwnerReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PendingBalance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ReservedBalance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PaidOutBalance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    DebtBalance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DebitAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SourceType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReversalOfEntryId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialLedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AccountType",
                table: "FinancialAccounts",
                column: "AccountType",
                unique: true,
                filter: "\"AccountType\" = 'Platform'");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_AccountType_OwnerReferenceId",
                table: "FinancialAccounts",
                columns: new[] { "AccountType", "OwnerReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_CreditAccountId",
                table: "FinancialLedgerEntries",
                column: "CreditAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_DebitAccountId",
                table: "FinancialLedgerEntries",
                column: "DebitAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_SourceType_SourceId",
                table: "FinancialLedgerEntries",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_SourceType_SourceId_EntryType",
                table: "FinancialLedgerEntries",
                columns: new[] { "SourceType", "SourceId", "EntryType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialLedgerEntries_TransactionNumber",
                table: "FinancialLedgerEntries",
                column: "TransactionNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialAccounts");

            migrationBuilder.DropTable(
                name: "FinancialLedgerEntries");

            migrationBuilder.DropColumn(
                name: "TaxRateApplied",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxRuleId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TaxRuleName",
                table: "Invoices");
        }
    }
}
