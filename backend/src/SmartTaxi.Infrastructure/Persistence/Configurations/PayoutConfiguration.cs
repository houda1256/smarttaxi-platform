using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");

        builder.HasKey(payout => payout.Id);
        builder.Property(payout => payout.Id).ValueGeneratedNever();

        builder.Property(payout => payout.BeneficiaryAccountId).IsRequired();
        builder.HasIndex(payout => payout.BeneficiaryAccountId);

        builder.Property(payout => payout.BeneficiaryType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(payout => payout.Amount).HasPrecision(10, 2).IsRequired();
        builder.Property(payout => payout.Currency).HasMaxLength(3).IsRequired();
        builder.Property(payout => payout.Method).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payout => payout.Frequency).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(payout => payout.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(payout => payout.Status);

        builder.Property(payout => payout.RequestedAt).IsRequired();
        builder.Property(payout => payout.ApprovedBy);
        builder.Property(payout => payout.ApprovedAt);
        builder.Property(payout => payout.ProcessedAt);
        builder.Property(payout => payout.PaidAt);
        builder.Property(payout => payout.FailureReason).HasMaxLength(1000);

        builder.Property(payout => payout.CreatedAt).IsRequired();
        builder.Property(payout => payout.UpdatedAt).IsRequired();
    }
}
