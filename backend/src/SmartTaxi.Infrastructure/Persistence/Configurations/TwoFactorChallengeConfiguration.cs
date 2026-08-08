using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class TwoFactorChallengeConfiguration : IEntityTypeConfiguration<TwoFactorChallenge>
{
    public void Configure(EntityTypeBuilder<TwoFactorChallenge> builder)
    {
        builder.ToTable("TwoFactorChallenges");

        builder.HasKey(challenge => challenge.Id);
        builder.Property(challenge => challenge.Id).ValueGeneratedNever();

        builder.Property(challenge => challenge.UserId).IsRequired();
        builder.HasIndex(challenge => challenge.UserId);

        builder.Property(challenge => challenge.TokenHash).HasMaxLength(500).IsRequired();
        builder.HasIndex(challenge => challenge.TokenHash).IsUnique();

        builder.Property(challenge => challenge.DeviceLabel).HasMaxLength(200);

        builder.Property(challenge => challenge.CreatedAt).IsRequired();
        builder.Property(challenge => challenge.ExpiresAt).IsRequired();
    }
}
