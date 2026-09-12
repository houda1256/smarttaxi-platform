using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.ToTable("Receipts");

        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Id).ValueGeneratedNever();

        builder.Property(receipt => receipt.PaymentId).IsRequired();
        builder.HasIndex(receipt => receipt.PaymentId).IsUnique();

        builder.Property(receipt => receipt.ReceiptNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(receipt => receipt.ReceiptNumber).IsUnique();

        builder.Property(receipt => receipt.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(receipt => receipt.Currency).HasMaxLength(3).IsRequired();
        builder.Property(receipt => receipt.IssuedAt).IsRequired();
        builder.Property(receipt => receipt.PdfStorageKey).HasMaxLength(500);
    }
}
