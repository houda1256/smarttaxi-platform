using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyRedemptionConfiguration : IEntityTypeConfiguration<LoyaltyRedemption>
{
    public void Configure(EntityTypeBuilder<LoyaltyRedemption> builder)
    {
        builder.ToTable("LoyaltyRedemptions");

        builder.HasKey(redemption => redemption.Id);
        builder.Property(redemption => redemption.Id).ValueGeneratedNever();

        builder.Property(redemption => redemption.LoyaltyAccountId).IsRequired();
        builder.Property(redemption => redemption.UserId).IsRequired();
        builder.Property(redemption => redemption.RewardId).IsRequired();
        builder.Property(redemption => redemption.PointsSpent).IsRequired();
        builder.Property(redemption => redemption.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(redemption => redemption.RedeemedAtUtc).IsRequired();

        // Scoped per-user (not global) so two different users can never collide on the same client-chosen
        // key — see the entity's own doc comment and the Module 7 audit fix #3.
        builder.HasIndex(redemption => new { redemption.UserId, redemption.IdempotencyKey }).IsUnique();

        builder.HasIndex(redemption => new { redemption.UserId, redemption.RedeemedAtUtc });
    }
}
