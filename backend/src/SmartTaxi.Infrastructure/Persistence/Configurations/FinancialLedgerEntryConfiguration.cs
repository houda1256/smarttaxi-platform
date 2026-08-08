using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Ledger.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FinancialLedgerEntryConfiguration : IEntityTypeConfiguration<FinancialLedgerEntry>
{
    public void Configure(EntityTypeBuilder<FinancialLedgerEntry> builder)
    {
        builder.ToTable("FinancialLedgerEntries");

        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.TransactionNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(entry => entry.TransactionNumber).IsUnique();

        builder.Property(entry => entry.DebitAccountId).IsRequired();
        builder.HasIndex(entry => entry.DebitAccountId);

        builder.Property(entry => entry.CreditAccountId).IsRequired();
        builder.HasIndex(entry => entry.CreditAccountId);

        builder.Property(entry => entry.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(entry => entry.Currency).HasMaxLength(3).IsRequired();

        builder.Property(entry => entry.EntryType).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(entry => entry.SourceType).HasMaxLength(50).IsRequired();
        builder.Property(entry => entry.SourceId).IsRequired();
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceId });

        // Prevents duplicate posting of the same logical event at the database level, on top of the
        // application-level idempotency check in IFinancialLedgerRepository.PostBatchAsync.
        builder.HasIndex(entry => new { entry.SourceType, entry.SourceId, entry.EntryType }).IsUnique();

        builder.Property(entry => entry.Description).HasMaxLength(1000);
        builder.Property(entry => entry.CreatedAt).IsRequired();
        builder.Property(entry => entry.CreatedBy);
        builder.Property(entry => entry.ReversalOfEntryId);
    }
}
