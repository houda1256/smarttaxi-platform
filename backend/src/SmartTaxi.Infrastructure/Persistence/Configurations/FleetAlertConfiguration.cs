using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Alerts.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FleetAlertConfiguration : IEntityTypeConfiguration<FleetAlert>
{
    public void Configure(EntityTypeBuilder<FleetAlert> builder)
    {
        builder.ToTable("FleetAlerts");

        builder.HasKey(alert => alert.Id);
        builder.Property(alert => alert.Id).ValueGeneratedNever();

        builder.Property(alert => alert.OwnerId).IsRequired();
        builder.HasIndex(alert => alert.OwnerId);

        builder.Property(alert => alert.FleetId);
        builder.Property(alert => alert.AlertType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(alert => alert.RelatedEntityId);

        builder.Property(alert => alert.Message).HasMaxLength(1000).IsRequired();
        builder.Property(alert => alert.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(alert => alert.CreatedAt).IsRequired();
        builder.Property(alert => alert.ResolvedAt);

        builder.HasIndex(alert => new { alert.OwnerId, alert.AlertType, alert.RelatedEntityId, alert.Status });
    }
}
