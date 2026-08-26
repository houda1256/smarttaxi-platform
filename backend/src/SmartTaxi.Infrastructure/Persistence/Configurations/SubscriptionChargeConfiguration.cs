using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.SubscriptionCharges.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionChargeConfiguration : IEntityTypeConfiguration<SubscriptionCharge>
{
    public void Configure(EntityTypeBuilder<SubscriptionCharge> builder)
    {
        builder.ToTable("SubscriptionCharges");

        builder.HasKey(charge => charge.Id);
        builder.Property(charge => charge.Id).ValueGeneratedNever();

        builder.Property(charge => charge.SubscriberId).IsRequired();
        builder.HasIndex(charge => charge.SubscriberId);

        builder.Property(charge => charge.SubscriptionId).IsRequired();
        builder.HasIndex(charge => charge.SubscriptionId);

        builder.Property(charge => charge.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(charge => charge.Currency).HasMaxLength(3).IsRequired();
        builder.Property(charge => charge.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(charge => charge.FailureReason).HasMaxLength(500);

        builder.Property(charge => charge.CreatedAt).IsRequired();
        builder.Property(charge => charge.UpdatedAt).IsRequired();
    }
}
