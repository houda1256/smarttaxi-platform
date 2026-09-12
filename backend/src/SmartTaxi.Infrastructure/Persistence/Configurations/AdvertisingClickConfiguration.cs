using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdvertisingClickConfiguration : IEntityTypeConfiguration<AdvertisingClick>
{
    public void Configure(EntityTypeBuilder<AdvertisingClick> builder)
    {
        builder.ToTable("AdvertisingClicks");

        builder.HasKey(click => click.Id);
        builder.Property(click => click.Id).ValueGeneratedNever();

        builder.Property(click => click.CampaignId).IsRequired();
        builder.Property(click => click.PlacementId).IsRequired();
        builder.Property(click => click.ImpressionId);

        builder.Property(click => click.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(click => click.IdempotencyKey).IsUnique();

        builder.Property(click => click.OperationalCost).HasPrecision(12, 4).IsRequired();
        builder.Property(click => click.OccurredAtUtc).IsRequired();
        builder.Property(click => click.CreatedAtUtc).IsRequired();

        builder.HasIndex(click => new { click.CampaignId, click.OccurredAtUtc });
    }
}
