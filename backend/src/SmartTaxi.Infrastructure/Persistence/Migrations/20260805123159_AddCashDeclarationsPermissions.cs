using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashDeclarationsPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Driver: submits their own periodic cash declarations.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Driver", "finance.cash-declarations.submit.own" }
                });

            // Admin: reviews, approves, disputes, and settles any declaration — the review queue itself is
            // global (ICashDeclarationRepository.GetForReviewAsync has no owner scoping), matching the
            // Admin-only pattern already used for payouts.manage/cash-register.audit.
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "finance.cash-declarations.review" }
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
                    { "Driver", "finance.cash-declarations.submit.own" },
                    { "Admin", "finance.cash-declarations.review" }
                });
        }
    }
}
