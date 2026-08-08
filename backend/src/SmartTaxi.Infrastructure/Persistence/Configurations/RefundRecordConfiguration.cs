using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RefundRecordConfiguration : IEntityTypeConfiguration<RefundRecord>
{
    public void Configure(EntityTypeBuilder<RefundRecord> builder)
    {
        builder.ToTable("RefundRecords");

        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();

        builder.Property(record => record.PaymentId).IsRequired();
        builder.HasIndex(record => record.PaymentId);

        builder.Property(record => record.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(record => record.Currency).HasMaxLength(3).IsRequired();
        builder.Property(record => record.RefundType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(record => record.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(record => record.RequestedBy).IsRequired();
        builder.Property(record => record.ProcessedAt).IsRequired();
    }
}
