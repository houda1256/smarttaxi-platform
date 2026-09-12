using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiRoleAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.Role, x.PermissionCode });
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.Role });
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Backward compatibility: copy every existing user's current single
            // role into the new UserRoles table BEFORE the scalar column below is
            // dropped, so no existing user loses their role.
            migrationBuilder.Sql(
                """
                INSERT INTO "UserRoles" ("UserId", "Role")
                SELECT "Id", "Role" FROM "Users";
                """);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // Default role -> permission mapping (least privilege: Customer and
            // Driver get no elevated permissions by default).
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "users.read" },
                    { "Admin", "users.manage" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            // Best-effort restore: if a user had gained more than one role while
            // on the new schema, this picks an arbitrary one of them (a full
            // rollback to single-role cannot be lossless in that case).
            migrationBuilder.Sql(
                """
                UPDATE "Users" u
                SET "Role" = ur."Role"
                FROM "UserRoles" ur
                WHERE ur."UserId" = u."Id";
                """);

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");
        }
    }
}
