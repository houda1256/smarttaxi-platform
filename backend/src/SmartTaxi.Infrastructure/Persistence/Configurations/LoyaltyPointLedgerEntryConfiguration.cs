using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyPointLedgerEntryConfiguration : IEntityTypeConfiguration<LoyaltyPointLedgerEntry>
{
    public void Configure(EntityTypeBuilder<LoyaltyPointLedgerEntry> builder)
    {
        builder.ToTable("LoyaltyPointLedgerEntries");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.LoyaltyAccountId).IsRequired();
        builder.Property(entry => entry.UserId).IsRequired();
        builder.Property(entry => entry.PointType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entry => entry.EntryType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(entry => entry.Points).IsRequired();
        builder.Property(entry => entry.BalanceAfter).IsRequired();
        builder.Property(entry => entry.SourceType).HasMaxLength(100).IsRequired();
        builder.Property(entry => entry.SourceId).IsRequired();
        builder.Property(entry => entry.EarningRuleId);
        builder.Property(entry => entry.RewardId);
        builder.Property(entry => entry.ReferralId);
        builder.Property(entry => entry.Reason).HasMaxLength(500).IsRequired();
        builder.Property(entry => entry.ExpirationAtUtc);
        builder.Property(entry => entry.CreatedBy);
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();

        builder.Property(entry => entry.IdempotencyKey).HasMaxLength(400).IsRequired();
        builder.HasIndex(entry => entry.IdempotencyKey).IsUnique();

        // Operational lot-tracking state (see the property's own doc comment) — non-null only for
        // Earn/RewardPoints rows, which are the only rows that represent a spendable/expirable lot.
        builder.Property(entry => entry.RemainingAmount);
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_LoyaltyPointLedgerEntries_RemainingAmount_NonNegative", "\"RemainingAmount\" IS NULL OR \"RemainingAmount\" >= 0"));

        builder.HasIndex(entry => new { entry.UserId, entry.CreatedAtUtc });
        builder.HasIndex(entry => entry.ExpirationAtUtc);
    }
}
