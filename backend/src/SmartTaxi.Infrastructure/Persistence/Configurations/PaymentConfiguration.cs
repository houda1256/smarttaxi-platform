using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Payments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();

        builder.Property(payment => payment.RideId).IsRequired();
        builder.HasIndex(payment => payment.RideId);

        builder.Property(payment => payment.RideNumber).HasMaxLength(30).IsRequired();
        builder.Property(payment => payment.CustomerId).IsRequired();
        builder.HasIndex(payment => payment.CustomerId);

        builder.Property(payment => payment.DriverId).IsRequired();
        builder.HasIndex(payment => payment.DriverId);

        builder.Property(payment => payment.VehicleId).IsRequired();
        builder.Property(payment => payment.OwnerId).IsRequired();
        builder.HasIndex(payment => payment.OwnerId);

        builder.Property(payment => payment.PaymentReference).HasMaxLength(30).IsRequired();
        builder.HasIndex(payment => payment.PaymentReference).IsUnique();

        builder.Property(payment => payment.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(payment => payment.Status);

        builder.Property(payment => payment.EstimatedFareAmount).HasPrecision(10, 2);
        builder.Property(payment => payment.FinalFareAmount).HasPrecision(10, 2).IsRequired();
        builder.Property(payment => payment.Currency).HasMaxLength(3).IsRequired();

        builder.Property(payment => payment.PlatformCommissionAmount).HasPrecision(10, 2);
        builder.Property(payment => payment.DriverAmount).HasPrecision(10, 2);
        builder.Property(payment => payment.OwnerAmount).HasPrecision(10, 2);
        builder.Property(payment => payment.RefundedAmount).HasPrecision(10, 2).IsRequired();

        builder.Property(payment => payment.CreatedAt).IsRequired();
        builder.Property(payment => payment.AuthorizedAt);
        builder.Property(payment => payment.ConfirmedAt);
        builder.HasIndex(payment => payment.ConfirmedAt);
        builder.Property(payment => payment.CancelledAt);
        builder.Property(payment => payment.FailedAt);
        builder.Property(payment => payment.UpdatedAt).IsRequired();

        // The "no duplicate payment" guarantee: only one non-terminal Payment may exist per Ride at a time.
        builder.HasIndex(payment => payment.RideId)
            .IsUnique()
            .HasFilter("\"Status\" NOT IN ('Failed', 'Cancelled', 'Refunded')");
    }
}
