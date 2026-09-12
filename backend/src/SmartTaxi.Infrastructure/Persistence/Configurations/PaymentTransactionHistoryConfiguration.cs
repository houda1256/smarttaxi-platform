using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionHistoryConfiguration : IEntityTypeConfiguration<PaymentTransactionHistory>
{
    public void Configure(EntityTypeBuilder<PaymentTransactionHistory> builder)
    {
        builder.ToTable("PaymentTransactionHistories");

        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();

        builder.Property(history => history.PaymentId).IsRequired();
        builder.HasIndex(history => history.PaymentId);

        builder.Property(history => history.PreviousStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(history => history.NewStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(history => history.ChangedBy);
        builder.Property(history => history.Reason).HasMaxLength(500);
        builder.Property(history => history.Amount).HasPrecision(10, 2);
        builder.Property(history => history.ChangedAt).IsRequired();
    }
}
