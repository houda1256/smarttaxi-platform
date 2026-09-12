using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyReferralRewardConfiguration : IEntityTypeConfiguration<LoyaltyReferralReward>
{
    public void Configure(EntityTypeBuilder<LoyaltyReferralReward> builder)
    {
        builder.ToTable("LoyaltyReferralRewards");

        builder.HasKey(reward => reward.Id);
        builder.Property(reward => reward.Id).ValueGeneratedNever();

        builder.Property(reward => reward.ReferralId).IsRequired();
        builder.HasIndex(reward => reward.ReferralId).IsUnique();

        builder.Property(reward => reward.ReferrerUserId).IsRequired();
        builder.HasIndex(reward => reward.ReferrerUserId);

        builder.Property(reward => reward.RefereeUserId).IsRequired();
        builder.Property(reward => reward.ReferrerRewardPoints).IsRequired();
        builder.Property(reward => reward.RefereeRewardPoints).IsRequired();
        builder.Property(reward => reward.RuleCode).HasMaxLength(50).IsRequired();
        builder.Property(reward => reward.GrantedAtUtc).IsRequired();
    }
}
