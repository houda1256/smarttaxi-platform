using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class GroupedInvoiceConfiguration : IEntityTypeConfiguration<GroupedInvoice>
{
    public void Configure(EntityTypeBuilder<GroupedInvoice> builder)
    {
        builder.ToTable("GroupedInvoices");

        builder.HasKey(invoice => invoice.Id);
        builder.Property(invoice => invoice.Id).ValueGeneratedNever();

        builder.Property(invoice => invoice.BusinessCustomerId).IsRequired();
        builder.HasIndex(invoice => invoice.BusinessCustomerId);

        builder.Property(invoice => invoice.InvoiceNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(invoice => invoice.InvoiceNumber).IsUnique();

        builder.Property(invoice => invoice.PeriodType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(invoice => invoice.PeriodStart).IsRequired();
        builder.Property(invoice => invoice.PeriodEnd).IsRequired();

        builder.Property(invoice => invoice.Subtotal).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.TaxAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.TotalAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(invoice => invoice.Currency).HasMaxLength(3).IsRequired();

        builder.Property(invoice => invoice.IssueDate).IsRequired();
        builder.Property(invoice => invoice.DueDate).IsRequired();

        builder.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(invoice => invoice.Status);

        builder.Property(invoice => invoice.CreatedAt).IsRequired();
    }
}
