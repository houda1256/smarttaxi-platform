using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class CashRegisterSessionConfiguration : IEntityTypeConfiguration<CashRegisterSession>
{
    public void Configure(EntityTypeBuilder<CashRegisterSession> builder)
    {
        builder.ToTable("CashRegisterSessions");

        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).ValueGeneratedNever();

        builder.Property(session => session.CashRegisterId).IsRequired();
        builder.HasIndex(session => session.CashRegisterId);

        builder.Property(session => session.OpenedBy).IsRequired();
        builder.Property(session => session.OpenedAt).IsRequired();
        builder.Property(session => session.OpeningBalance).HasPrecision(10, 2).IsRequired();
        builder.Property(session => session.ClosingExpectedBalance).HasPrecision(10, 2);
        builder.Property(session => session.ClosingActualBalance).HasPrecision(10, 2);
        builder.Property(session => session.Difference).HasPrecision(10, 2);
        builder.Property(session => session.DifferenceReason).HasMaxLength(1000);
        builder.Property(session => session.ClosedAt);

        builder.Property(session => session.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(session => session.Status);

        // "Only one Open session per register at a time" — the atomic DB-level guard TryAddAsync relies on.
        builder.HasIndex(session => session.CashRegisterId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Open'");
    }
}
