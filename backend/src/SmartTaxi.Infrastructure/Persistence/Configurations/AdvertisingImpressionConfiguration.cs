using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class AdvertisingImpressionConfiguration : IEntityTypeConfiguration<AdvertisingImpression>
{
    public void Configure(EntityTypeBuilder<AdvertisingImpression> builder)
    {
        builder.ToTable("AdvertisingImpressions");

        builder.HasKey(impression => impression.Id);
        builder.Property(impression => impression.Id).ValueGeneratedNever();

        builder.Property(impression => impression.CampaignId).IsRequired();
        builder.Property(impression => impression.PlacementId).IsRequired();

        builder.Property(impression => impression.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.HasIndex(impression => impression.IdempotencyKey).IsUnique();

        builder.Property(impression => impression.OperationalCost).HasPrecision(12, 4).IsRequired();
        builder.Property(impression => impression.OccurredAtUtc).IsRequired();
        builder.Property(impression => impression.CreatedAtUtc).IsRequired();

        builder.HasIndex(impression => new { impression.CampaignId, impression.OccurredAtUtc });
    }
}
