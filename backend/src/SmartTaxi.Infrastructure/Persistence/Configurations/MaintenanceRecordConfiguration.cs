using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.ValueObjects;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceRecordConfiguration : IEntityTypeConfiguration<MaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<MaintenanceRecord> builder)
    {
        builder.ToTable("MaintenanceRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.MaintenanceRequestId).IsRequired();
        builder.HasIndex(record => record.MaintenanceRequestId).IsUnique();

        builder.Property(record => record.VehicleId).IsRequired();
        builder.HasIndex(record => record.VehicleId);

        builder.Property(record => record.OwnerUserId).IsRequired();
        builder.Property(record => record.GarageUserId).IsRequired();

        builder.Property(record => record.InterventionDate).IsRequired();
        builder.Property(record => record.MileageAtCompletion);

        builder.Property(record => record.FinalCost).HasPrecision(12, 3).IsRequired();
        builder.Property(record => record.Notes).HasMaxLength(2000);
        builder.Property(record => record.NextRecommendedServiceDate);
        builder.HasIndex(record => record.NextRecommendedServiceDate);
        builder.Property(record => record.WarrantyInfo).HasMaxLength(500);
        builder.Property(record => record.CreatedAtUtc).IsRequired();

        // The public read-only Lines projection is the same CLR collection type as the owned entity
        // itself, which would otherwise make EF's convention-based discovery ambiguous between it and the
        // private _lines field below — explicitly ignored so only the field is configured as a navigation
        // (same reason User.Roles returns plain UserRole values rather than UserRoleAssignment entities).
        builder.Ignore(record => record.Lines);

        // Immutable digital-maintenance-book entry: no repository Update/Delete method exists at all (see
        // IMaintenanceRecordRepository) — this table is insert-only by construction, not merely by discipline.
        builder.OwnsMany<MaintenanceRecordLine>("_lines", lines =>
        {
            lines.ToTable("MaintenanceRecordLines");
            lines.WithOwner().HasForeignKey("MaintenanceRecordId");
            lines.Property<Guid>("Id").ValueGeneratedOnAdd();
            lines.HasKey("Id");

            lines.Property(line => line.Description).HasMaxLength(500).IsRequired();
            lines.Property(line => line.IsPart).IsRequired();
            lines.Property(line => line.Quantity).IsRequired();
            lines.Property(line => line.UnitCost).HasPrecision(12, 3).IsRequired();
        });

        builder.Navigation("_lines").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ToTable(t => t.HasCheckConstraint("CK_MaintenanceRecords_FinalCost_NonNegative", "\"FinalCost\" >= 0"));
    }
}
