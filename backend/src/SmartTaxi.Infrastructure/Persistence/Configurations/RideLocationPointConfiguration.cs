using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideLocationPointConfiguration : IEntityTypeConfiguration<RideLocationPoint>
{
    public void Configure(EntityTypeBuilder<RideLocationPoint> builder)
    {
        builder.ToTable("RideLocationPoints");

        builder.HasKey(point => point.Id);
        builder.Property(point => point.Id).ValueGeneratedNever();

        builder.Property(point => point.RideId).IsRequired();
        builder.HasIndex(point => new { point.RideId, point.RecordedAt });

        builder.Property(point => point.DriverId).IsRequired();
        builder.Property(point => point.Latitude).IsRequired();
        builder.Property(point => point.Longitude).IsRequired();
        builder.Property(point => point.Speed);
        builder.Property(point => point.Heading);
        builder.Property(point => point.Accuracy);
        builder.Property(point => point.RecordedAt).IsRequired();

        // Retention pruning scans this column directly (see IRideLocationPointRepository.PruneOlderThanAsync).
        builder.HasIndex(point => point.RecordedAt);
    }
}
