using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideShareTokenConfiguration : IEntityTypeConfiguration<RideShareToken>
{
    public void Configure(EntityTypeBuilder<RideShareToken> builder)
    {
        builder.ToTable("RideShareTokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.RideId).IsRequired();
        builder.HasIndex(token => token.RideId);

        builder.Property(token => token.TokenHash).HasMaxLength(200).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();
        builder.Property(token => token.RevokedAt);
    }
}
