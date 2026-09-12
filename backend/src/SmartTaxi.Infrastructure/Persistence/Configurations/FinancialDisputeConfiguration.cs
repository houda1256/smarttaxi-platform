using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class FinancialDisputeConfiguration : IEntityTypeConfiguration<FinancialDispute>
{
    public void Configure(EntityTypeBuilder<FinancialDispute> builder)
    {
        builder.ToTable("FinancialDisputes");

        builder.HasKey(dispute => dispute.Id);
        builder.Property(dispute => dispute.Id).ValueGeneratedNever();

        builder.Property(dispute => dispute.Category).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(dispute => dispute.RelatedPaymentId);
        builder.HasIndex(dispute => dispute.RelatedPaymentId);
        builder.Property(dispute => dispute.RelatedInvoiceId);
        builder.HasIndex(dispute => dispute.RelatedInvoiceId);
        builder.Property(dispute => dispute.RelatedPayoutId);
        builder.HasIndex(dispute => dispute.RelatedPayoutId);

        builder.Property(dispute => dispute.DisputedAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(dispute => dispute.Currency).HasMaxLength(3).IsRequired();
        builder.Property(dispute => dispute.Description).HasMaxLength(2000).IsRequired();
        builder.Property(dispute => dispute.EvidenceReference).HasMaxLength(500);

        builder.Property(dispute => dispute.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(dispute => dispute.Status);

        builder.Property(dispute => dispute.RaisedBy).IsRequired();
        builder.HasIndex(dispute => dispute.RaisedBy);

        builder.Property(dispute => dispute.AssignedFinanceManagerId);
        builder.Property(dispute => dispute.Resolution).HasMaxLength(2000);
        builder.Property(dispute => dispute.ResolvedAt);

        builder.Property(dispute => dispute.CreatedAt).IsRequired();
        builder.Property(dispute => dispute.UpdatedAt).IsRequired();
    }
}
