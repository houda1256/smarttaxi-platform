using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.BusinessCustomers.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class BusinessCustomerConfiguration : IEntityTypeConfiguration<BusinessCustomer>
{
    public void Configure(EntityTypeBuilder<BusinessCustomer> builder)
    {
        builder.ToTable("BusinessCustomers");

        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();

        builder.Property(customer => customer.LegalName).HasMaxLength(300).IsRequired();
        builder.Property(customer => customer.TaxIdentifier).HasMaxLength(50).IsRequired();
        builder.HasIndex(customer => customer.TaxIdentifier).IsUnique();

        builder.Property(customer => customer.BillingAddress).HasMaxLength(500).IsRequired();
        builder.Property(customer => customer.ContactPersonName).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.ContactPersonEmail).HasMaxLength(320).IsRequired();
        builder.Property(customer => customer.ContactPersonPhone).HasMaxLength(20);

        builder.Property(customer => customer.PaymentTerms).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(customer => customer.CreditLimit).HasPrecision(10, 2).IsRequired();
        builder.Property(customer => customer.CurrentCreditUsage).HasPrecision(10, 2).IsRequired();

        builder.Property(customer => customer.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(customer => customer.Status);

        builder.Property(customer => customer.CreatedAt).IsRequired();
        builder.Property(customer => customer.UpdatedAt).IsRequired();
    }
}
