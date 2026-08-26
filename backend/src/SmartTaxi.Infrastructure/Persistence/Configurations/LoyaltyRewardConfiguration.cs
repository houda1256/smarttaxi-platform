using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyRewardConfiguration : IEntityTypeConfiguration<LoyaltyReward>
{
    public void Configure(EntityTypeBuilder<LoyaltyReward> builder)
    {
        builder.ToTable("LoyaltyRewards");

        builder.HasKey(reward => reward.Id);
        builder.Property(reward => reward.Id).ValueGeneratedNever();

        builder.Property(reward => reward.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(reward => reward.Code).IsUnique();

        builder.Property(reward => reward.Name).HasMaxLength(200).IsRequired();
        builder.Property(reward => reward.Description).HasMaxLength(2000).IsRequired();
        builder.Property(reward => reward.CostInRewardPoints).IsRequired();
        builder.Property(reward => reward.RewardType).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Reassigned wholesale (never mutated in place) — reference-based change tracking is enough, same convention as SubscriptionPlan.Features.
        builder.Property(reward => reward.TargetRoles)
            .HasConversion(
                roles => string.Join('|', roles),
                value => value.Length == 0
                    ? new List<UserRole>()
                    : value.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(Enum.Parse<UserRole>).ToList())
            .HasColumnType("text");

        builder.Property(reward => reward.IsActive).IsRequired();
        builder.Property(reward => reward.AvailableFrom);
        builder.Property(reward => reward.AvailableTo);
        builder.Property(reward => reward.UsageLimit);
        builder.Property(reward => reward.RedeemedCount).IsRequired();
        builder.Property(reward => reward.CreatedAtUtc).IsRequired();
        builder.Property(reward => reward.UpdatedAtUtc).IsRequired();

        builder.HasIndex(reward => reward.IsActive);
    }
}
