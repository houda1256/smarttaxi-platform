using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxesPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "finance.tax.manage" }
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
                    { "Admin", "finance.tax.manage" }
                });
        }
    }
}
