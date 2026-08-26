using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyChallengeConfiguration : IEntityTypeConfiguration<LoyaltyChallenge>
{
    public void Configure(EntityTypeBuilder<LoyaltyChallenge> builder)
    {
        builder.ToTable("LoyaltyChallenges");

        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).ValueGeneratedNever();

        builder.Property(challenge => challenge.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(challenge => challenge.Code).IsUnique();

        builder.Property(challenge => challenge.Name).HasMaxLength(200).IsRequired();
        builder.Property(challenge => challenge.CriteriaType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(challenge => challenge.TargetValue).IsRequired();
        builder.Property(challenge => challenge.RewardPoints).IsRequired();
        builder.Property(challenge => challenge.EligibleRole).HasConversion<string>().HasMaxLength(30);
        builder.Property(challenge => challenge.ValidFrom);
        builder.Property(challenge => challenge.ValidTo);
        builder.Property(challenge => challenge.IsActive).IsRequired();
        builder.Property(challenge => challenge.CreatedAtUtc).IsRequired();
        builder.Property(challenge => challenge.UpdatedAtUtc).IsRequired();

        builder.HasIndex(challenge => challenge.IsActive);
    }
}
