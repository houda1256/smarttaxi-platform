using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Add2dUserDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentAccessAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAccessAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FileReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ReplacesDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentAccessAuditEntries_DocumentId",
                table: "DocumentAccessAuditEntries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_Status",
                table: "UserDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserId",
                table: "UserDocuments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserId_DocumentType_Sha256",
                table: "UserDocuments",
                columns: new[] { "UserId", "DocumentType", "Sha256" });

            // Admin gets the full document-review permission set (added to its
            // existing users.read/users.manage rows from AddMultiRoleAndPermissions).
            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Role", "PermissionCode" },
                values: new object[,]
                {
                    { "Admin", "documents.read.all" },
                    { "Admin", "documents.review" },
                    { "Admin", "documents.suspend" },
                    { "Admin", "documents.configure" },
                    { "Driver", "documents.read.own" },
                    { "Driver", "documents.upload.own" },
                    { "TaxiOwner", "documents.read.own" },
                    { "TaxiOwner", "documents.upload.own" },
                    { "GaragePartner", "documents.read.own" },
                    { "GaragePartner", "documents.upload.own" },
                    { "RoadsideAssistancePartner", "documents.read.own" },
                    { "RoadsideAssistancePartner", "documents.upload.own" },
                    { "Advertiser", "documents.read.own" },
                    { "Advertiser", "documents.upload.own" },
                    { "BusinessCustomer", "documents.read.own" },
                    { "BusinessCustomer", "documents.upload.own" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentAccessAuditEntries");

            migrationBuilder.DropTable(
                name: "UserDocuments");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "Role", "PermissionCode" },
                keyValues: new object[,]
                {
                    { "Admin", "documents.read.all" },
                    { "Admin", "documents.review" },
                    { "Admin", "documents.suspend" },
                    { "Admin", "documents.configure" },
                    { "Driver", "documents.read.own" },
                    { "Driver", "documents.upload.own" },
                    { "TaxiOwner", "documents.read.own" },
                    { "TaxiOwner", "documents.upload.own" },
                    { "GaragePartner", "documents.read.own" },
                    { "GaragePartner", "documents.upload.own" },
                    { "RoadsideAssistancePartner", "documents.read.own" },
                    { "RoadsideAssistancePartner", "documents.upload.own" },
                    { "Advertiser", "documents.read.own" },
                    { "Advertiser", "documents.upload.own" },
                    { "BusinessCustomer", "documents.read.own" },
                    { "BusinessCustomer", "documents.upload.own" }
                });
        }
    }
}
