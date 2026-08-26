using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyTierThresholdConfiguration : IEntityTypeConfiguration<LoyaltyTierThreshold>
{
    public void Configure(EntityTypeBuilder<LoyaltyTierThreshold> builder)
    {
        builder.ToTable("LoyaltyTierThresholds");

        builder.HasKey(threshold => threshold.Id);
        builder.Property(threshold => threshold.Id).ValueGeneratedNever();

        builder.Property(threshold => threshold.Tier).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(threshold => threshold.Tier).IsUnique();

        builder.Property(threshold => threshold.MinimumStatusPoints).IsRequired();
        builder.Property(threshold => threshold.CreatedAtUtc).IsRequired();
        builder.Property(threshold => threshold.UpdatedAtUtc).IsRequired();
    }
}
