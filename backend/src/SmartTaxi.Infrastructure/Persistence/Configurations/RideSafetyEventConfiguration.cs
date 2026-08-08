using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideSafetyEventConfiguration : IEntityTypeConfiguration<RideSafetyEvent>
{
    public void Configure(EntityTypeBuilder<RideSafetyEvent> builder)
    {
        builder.ToTable("RideSafetyEvents");

        builder.HasKey(safetyEvent => safetyEvent.Id);
        builder.Property(safetyEvent => safetyEvent.Id).ValueGeneratedNever();

        builder.Property(safetyEvent => safetyEvent.RideId).IsRequired();
        builder.HasIndex(safetyEvent => safetyEvent.RideId);

        builder.Property(safetyEvent => safetyEvent.TriggeredByUserId).IsRequired();
        builder.Property(safetyEvent => safetyEvent.Latitude).IsRequired();
        builder.Property(safetyEvent => safetyEvent.Longitude).IsRequired();
        builder.Property(safetyEvent => safetyEvent.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(safetyEvent => safetyEvent.TriggeredAt).IsRequired();
        builder.Property(safetyEvent => safetyEvent.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(safetyEvent => safetyEvent.Status);
    }
}
