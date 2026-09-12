using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Accounts.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FinancialAccountConfiguration : IEntityTypeConfiguration<FinancialAccount>
{
    public void Configure(EntityTypeBuilder<FinancialAccount> builder)
    {
        builder.ToTable("FinancialAccounts");

        builder.HasKey(account => account.Id);
        builder.Property(account => account.Id).ValueGeneratedNever();

        builder.Property(account => account.AccountType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(account => account.OwnerReferenceId);
        builder.Property(account => account.Currency).HasMaxLength(3).IsRequired();

        builder.Property(account => account.PendingBalance).HasPrecision(10, 2).IsRequired();
        builder.Property(account => account.AvailableBalance).HasPrecision(10, 2).IsRequired();
        builder.Property(account => account.ReservedBalance).HasPrecision(10, 2).IsRequired();
        builder.Property(account => account.PaidOutBalance).HasPrecision(10, 2).IsRequired();
        builder.Property(account => account.DebtBalance).HasPrecision(10, 2).IsRequired();

        builder.Property(account => account.CreatedAt).IsRequired();
        builder.Property(account => account.UpdatedAt).IsRequired();

        // Every non-Platform account is unique per (AccountType, OwnerReferenceId) — enforced here since
        // OwnerReferenceId is never null for those rows.
        builder.HasIndex(account => new { account.AccountType, account.OwnerReferenceId }).IsUnique();

        // Postgres treats every NULL as distinct in a unique index, so the composite index above cannot
        // stop two Platform rows (OwnerReferenceId always null) from coexisting — this filtered index does.
        builder.HasIndex(account => account.AccountType)
            .IsUnique()
            .HasFilter("\"AccountType\" = 'Platform'");
    }
}
