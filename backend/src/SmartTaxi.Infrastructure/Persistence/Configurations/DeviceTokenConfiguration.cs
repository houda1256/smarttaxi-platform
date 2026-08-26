using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("DeviceTokens");

        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).ValueGeneratedNever();

        builder.Property(token => token.UserId).IsRequired();
        builder.HasIndex(token => token.UserId);

        builder.Property(token => token.Token).HasMaxLength(1000).IsRequired();
        builder.Property(token => token.Platform).HasMaxLength(30).IsRequired();

        builder.Property(token => token.CreatedAtUtc).IsRequired();
        builder.Property(token => token.LastUsedAtUtc).IsRequired();
        builder.Property(token => token.RevokedAtUtc);

        // "No two active rows for the same raw token" — a revoked row never blocks a fresh registration of the same token.
        builder.HasIndex(token => token.Token)
            .IsUnique()
            .HasFilter("\"RevokedAtUtc\" IS NULL");
    }
}
