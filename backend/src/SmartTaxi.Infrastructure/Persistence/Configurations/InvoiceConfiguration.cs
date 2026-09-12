using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedNever();

        builder.Property(invoice => invoice.PaymentId).IsRequired();
        builder.HasIndex(invoice => invoice.PaymentId).IsUnique();

        builder.Property(invoice => invoice.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();

        builder.Property(invoice => invoice.RideNumber).HasMaxLength(30).IsRequired();
        builder.Property(invoice => invoice.CustomerId).IsRequired();
        builder.Property(invoice => invoice.DriverId).IsRequired();
        builder.Property(invoice => invoice.OwnerId).IsRequired();

        builder.Property(invoice => invoice.Subtotal).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.TaxAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.TotalAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.Currency).HasMaxLength(3).IsRequired();

        builder.Property(invoice => invoice.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(invoice => invoice.IssueDate).IsRequired();
        builder.Property(invoice => invoice.PdfStorageKey).HasMaxLength(500);
        builder.Property(invoice => invoice.CreatedAt).IsRequired();
    }
}
