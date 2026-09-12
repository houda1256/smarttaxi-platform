using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTaxi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFleetModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverOwnerContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FixedAmount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    DriverPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    OwnerPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    PaymentFrequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DocumentReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverOwnerContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverLicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DriverLicenseExpiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TaxiLicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AvailabilityStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IndependentDriver = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentVehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    CompletedRideCount = table.Column<int>(type: "integer", nullable: false),
                    CancellationCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverVehicleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DaysOfWeek = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverVehicleAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: true),
                    AlertType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetExpenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: true),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: true),
                    Category = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ReceiptReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetExpenses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FleetMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetMembers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fleets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fleets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxiOwnerProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NationalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TradeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TaxIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegistrationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AddressStreet = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AddressCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AddressCountry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BankAccountHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BankAccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BankSwiftOrBic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AccountStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxiOwnerProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDocumentAccessAuditEntries",
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
                    table.PrimaryKey("PK_VehicleDocumentAccessAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_VehicleDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FleetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LicensePlate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Vin = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CurrentMileage = table.Column<int>(type: "integer", nullable: false),
                    FuelType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TransmissionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SeatCount = table.Column<int>(type: "integer", nullable: false),
                    HasAirConditioning = table.Column<bool>(type: "boolean", nullable: false),
                    IsAccessible = table.Column<bool>(type: "boolean", nullable: false),
                    VehicleCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperationalStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    VerificationStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MainPhotoReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VehicleUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    DriverId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MileageStart = table.Column<int>(type: "integer", nullable: false),
                    MileageEnd = table.Column<int>(type: "integer", nullable: false),
                    RideCount = table.Column<int>(type: "integer", nullable: false),
                    RevenueGenerated = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    IncidentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DriverOwnerContracts_DriverId",
                table: "DriverOwnerContracts",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverOwnerContracts_DriverId_OwnerId",
                table: "DriverOwnerContracts",
                columns: new[] { "DriverId", "OwnerId" },
                unique: true,
                filter: "\"Status\" IN ('PendingSignature', 'Active')");

            migrationBuilder.CreateIndex(
                name: "IX_DriverOwnerContracts_OwnerId",
                table: "DriverOwnerContracts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_UserId",
                table: "DriverProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_VerificationStatus",
                table: "DriverProfiles",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicleAssignments_DriverId",
                table: "DriverVehicleAssignments",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicleAssignments_OwnerId",
                table: "DriverVehicleAssignments",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicleAssignments_Status",
                table: "DriverVehicleAssignments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_DriverVehicleAssignments_VehicleId",
                table: "DriverVehicleAssignments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetAlerts_OwnerId",
                table: "FleetAlerts",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetAlerts_OwnerId_AlertType_RelatedEntityId_Status",
                table: "FleetAlerts",
                columns: new[] { "OwnerId", "AlertType", "RelatedEntityId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FleetExpenses_DriverId",
                table: "FleetExpenses",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetExpenses_ExpenseDate",
                table: "FleetExpenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_FleetExpenses_OwnerId",
                table: "FleetExpenses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetExpenses_Status",
                table: "FleetExpenses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FleetExpenses_VehicleId",
                table: "FleetExpenses",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetMembers_FleetId_UserId",
                table: "FleetMembers",
                columns: new[] { "FleetId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_OwnerId",
                table: "Fleets",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxiOwnerProfiles_UserId",
                table: "TaxiOwnerProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocumentAccessAuditEntries_DocumentId",
                table: "VehicleDocumentAccessAuditEntries",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_Status",
                table: "VehicleDocuments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId",
                table: "VehicleDocuments",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleDocuments_VehicleId_DocumentType_Sha256",
                table: "VehicleDocuments",
                columns: new[] { "VehicleId", "DocumentType", "Sha256" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_FleetId",
                table: "Vehicles",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_LicensePlate",
                table: "Vehicles",
                column: "LicensePlate",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OperationalStatus",
                table: "Vehicles",
                column: "OperationalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_OwnerId",
                table: "Vehicles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Vin",
                table: "Vehicles",
                column: "Vin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageRecords_AssignmentId",
                table: "VehicleUsageRecords",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageRecords_DriverId",
                table: "VehicleUsageRecords",
                column: "DriverId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleUsageRecords_VehicleId",
                table: "VehicleUsageRecords",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DriverOwnerContracts");

            migrationBuilder.DropTable(
                name: "DriverProfiles");

            migrationBuilder.DropTable(
                name: "DriverVehicleAssignments");

            migrationBuilder.DropTable(
                name: "FleetAlerts");

            migrationBuilder.DropTable(
                name: "FleetExpenses");

            migrationBuilder.DropTable(
                name: "FleetMembers");

            migrationBuilder.DropTable(
                name: "Fleets");

            migrationBuilder.DropTable(
                name: "TaxiOwnerProfiles");

            migrationBuilder.DropTable(
                name: "VehicleDocumentAccessAuditEntries");

            migrationBuilder.DropTable(
                name: "VehicleDocuments");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "VehicleUsageRecords");
        }
    }
}
