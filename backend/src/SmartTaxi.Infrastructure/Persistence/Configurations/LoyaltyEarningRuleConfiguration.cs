using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyEarningRuleConfiguration : IEntityTypeConfiguration<LoyaltyEarningRule>
{
    public void Configure(EntityTypeBuilder<LoyaltyEarningRule> builder)
    {
        builder.ToTable("LoyaltyEarningRules");

        builder.HasKey(rule => rule.Id);
        builder.Property(rule => rule.Id).ValueGeneratedNever();

        builder.Property(rule => rule.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(rule => rule.Code).IsUnique();

        builder.Property(rule => rule.ActorRole).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(rule => rule.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(rule => rule.RewardPointsPerCurrencyUnit).HasPrecision(18, 6).IsRequired();
        builder.Property(rule => rule.StatusPointsPerCurrencyUnit).HasPrecision(18, 6).IsRequired();
        builder.Property(rule => rule.MinPoints);
        builder.Property(rule => rule.MaxPoints);
        builder.Property(rule => rule.SubscriptionMultiplierAllowed).IsRequired();
        builder.Property(rule => rule.IsActive).IsRequired();
        builder.Property(rule => rule.ValidFrom);
        builder.Property(rule => rule.ValidTo);
        builder.Property(rule => rule.CreatedAtUtc).IsRequired();
        builder.Property(rule => rule.UpdatedAtUtc).IsRequired();

        builder.HasIndex(rule => new { rule.ActorRole, rule.SourceType, rule.IsActive });
    }
}
