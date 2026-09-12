using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("LoyaltyAccounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();

        builder.Property(account => account.UserId).IsRequired();
        builder.HasIndex(account => account.UserId).IsUnique();

        builder.Property(account => account.ActorRole).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(account => account.CurrentRewardPoints).IsRequired();
        builder.Property(account => account.CurrentStatusPoints).IsRequired();
        builder.Property(account => account.Tier).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(account => account.CreatedAtUtc).IsRequired();
        builder.Property(account => account.UpdatedAtUtc).IsRequired();
    }
}
