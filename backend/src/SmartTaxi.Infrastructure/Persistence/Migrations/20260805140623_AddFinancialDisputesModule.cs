using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDisputesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialDisputes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RelatedPaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedPayoutId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisputedAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EvidenceReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RaisedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedFinanceManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialDisputes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialDisputes_RaisedBy",
                table: "FinancialDisputes",
                column: "RaisedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialDisputes_RelatedInvoiceId",
                table: "FinancialDisputes",
                column: "RelatedInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialDisputes_RelatedPaymentId",
                table: "FinancialDisputes",
                column: "RelatedPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialDisputes_RelatedPayoutId",
                table: "FinancialDisputes",
                column: "RelatedPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialDisputes_Status",
                table: "FinancialDisputes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialDisputes");
        }
    }
}
