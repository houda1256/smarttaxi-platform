using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideStatusHistoryConfiguration : IEntityTypeConfiguration<RideStatusHistory>
{
    public void Configure(EntityTypeBuilder<RideStatusHistory> builder)
    {
        builder.ToTable("RideStatusHistories");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.RideId).IsRequired();
        builder.HasIndex(history => history.RideId);

        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(history => history.ChangedBy);
        builder.Property(history => history.Reason).HasMaxLength(500);
        builder.Property(history => history.ChangedAt).IsRequired();
    }
}
