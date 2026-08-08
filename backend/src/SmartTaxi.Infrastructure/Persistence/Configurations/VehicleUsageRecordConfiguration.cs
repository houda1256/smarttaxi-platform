using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.UsageHistory.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class VehicleUsageRecordConfiguration : IEntityTypeConfiguration<VehicleUsageRecord>
{
    public void Configure(EntityTypeBuilder<VehicleUsageRecord> builder)
    {
        builder.ToTable("VehicleUsageRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.VehicleId).IsRequired();
        builder.HasIndex(record => record.VehicleId);

        builder.Property(record => record.DriverId).IsRequired();
        builder.HasIndex(record => record.DriverId);

        builder.Property(record => record.AssignmentId).IsRequired();
        builder.HasIndex(record => record.AssignmentId);

        builder.Property(record => record.StartedAt).IsRequired();
        builder.Property(record => record.EndedAt).IsRequired();
        builder.Property(record => record.MileageStart).IsRequired();
        builder.Property(record => record.MileageEnd).IsRequired();
        builder.Property(record => record.RideCount).IsRequired();
        builder.Property(record => record.RevenueGenerated).HasPrecision(12, 2);
        builder.Property(record => record.IncidentCount).IsRequired();

        builder.Property(record => record.CreatedAt).IsRequired();
    }
}
