using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class GroupedInvoiceLineConfiguration : IEntityTypeConfiguration<GroupedInvoiceLine>
{
    public void Configure(EntityTypeBuilder<GroupedInvoiceLine> builder)
    {
        builder.ToTable("GroupedInvoiceLines");

        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.GroupedInvoiceId).IsRequired();
        builder.HasIndex(line => line.GroupedInvoiceId);

        builder.Property(line => line.RideId).IsRequired();

        // The database-level guarantee that a Ride can never be invoiced twice, across any GroupedInvoice.
        builder.HasIndex(line => line.RideId).IsUnique();

        builder.Property(line => line.RideNumber).HasMaxLength(30).IsRequired();
        builder.Property(line => line.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(line => line.CreatedAt).IsRequired();
    }
}
