using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyChallengeProgressConfiguration : IEntityTypeConfiguration<LoyaltyChallengeProgress>
{
    public void Configure(EntityTypeBuilder<LoyaltyChallengeProgress> builder)
    {
        builder.ToTable("LoyaltyChallengeProgress");

        builder.HasKey(progress => progress.Id);
        builder.Property(progress => progress.Id).ValueGeneratedNever();

        builder.Property(progress => progress.ChallengeId).IsRequired();
        builder.Property(progress => progress.UserId).IsRequired();
        builder.HasIndex(progress => new { progress.UserId, progress.ChallengeId }).IsUnique();

        builder.Property(progress => progress.CurrentValue).IsRequired();
        builder.Property(progress => progress.CompletedAtUtc);
        builder.Property(progress => progress.RewardedAtUtc);
        builder.Property(progress => progress.CreatedAtUtc).IsRequired();
        builder.Property(progress => progress.UpdatedAtUtc).IsRequired();
    }
}
