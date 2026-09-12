using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class RideConfiguration : IEntityTypeConfiguration<Ride>
{
    public void Configure(EntityTypeBuilder<Ride> builder)
    {
        builder.ToTable("Rides");

        builder.HasKey(ride => ride.Id);
        builder.Property(ride => ride.Id).ValueGeneratedNever();

        builder.Property(ride => ride.RideNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(ride => ride.RideNumber).IsUnique();

        builder.Property(ride => ride.CustomerId).IsRequired();
        builder.HasIndex(ride => ride.CustomerId);

        builder.Property(ride => ride.SelectedDriverId);
        builder.HasIndex(ride => ride.SelectedDriverId);
        builder.HasIndex(ride => new { ride.SelectedDriverId, ride.Status });

        builder.Property(ride => ride.VehicleId);
        builder.HasIndex(ride => ride.VehicleId);

        builder.Property(ride => ride.RideType).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(ride => ride.PickupAddress).HasMaxLength(500).IsRequired();
        builder.Property(ride => ride.DestinationAddress).HasMaxLength(500).IsRequired();

        // Safe here (unlike User.Email — see UserConfiguration): Ride is always
        // materialized via its parameterless private ctor, so PickupLocation/
        // DestinationLocation are never required as constructor arguments.
        builder.OwnsOne(ride => ride.PickupLocation, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("PickupLatitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("PickupLongitude").IsRequired();
        });

        builder.OwnsOne(ride => ride.DestinationLocation, location =>
        {
            location.Property(l => l.Latitude).HasColumnName("DestinationLatitude").IsRequired();
            location.Property(l => l.Longitude).HasColumnName("DestinationLongitude").IsRequired();
        });

        builder.Property(ride => ride.RequestedAt).IsRequired();
        builder.HasIndex(ride => ride.RequestedAt);

        builder.Property(ride => ride.ScheduledAt);
        builder.HasIndex(ride => ride.ScheduledAt);

        builder.Property(ride => ride.PassengerCount).IsRequired();
        builder.Property(ride => ride.LuggageCount).IsRequired();
        builder.Property(ride => ride.NeedsAirConditioning).IsRequired();
        builder.Property(ride => ride.NeedsAccessibleVehicle).IsRequired();
        builder.Property(ride => ride.HasChildSeatRequest).IsRequired();
        builder.Property(ride => ride.HasPet).IsRequired();
        builder.Property(ride => ride.PreferredVehicleCategory).HasConversion<string>().HasMaxLength(20);
        builder.Property(ride => ride.PreferredPaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(ride => ride.SpecialInstructions).HasMaxLength(1000);

        builder.Property(ride => ride.EstimatedDistanceKm).HasPrecision(8, 2);
        builder.Property(ride => ride.ActualDistanceKm).HasPrecision(8, 2);
        builder.Property(ride => ride.EstimatedFare).HasPrecision(10, 2);
        builder.Property(ride => ride.FinalFare).HasPrecision(10, 2);
        builder.Property(ride => ride.NegotiatedFinalFare).HasPrecision(10, 2);

        builder.Property(ride => ride.Currency).HasMaxLength(3).IsRequired();

        builder.Property(ride => ride.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasIndex(ride => ride.Status);
        builder.HasIndex(ride => new { ride.RideType, ride.Status });

        builder.Property(ride => ride.LastKnownLatitude);
        builder.Property(ride => ride.LastKnownLongitude);
        builder.Property(ride => ride.LastLocationRecordedAt);

        builder.Property(ride => ride.DriverArrivedAt);
        builder.Property(ride => ride.CancellationReason).HasMaxLength(500);

        builder.Property(ride => ride.CreatedAt).IsRequired();
        builder.Property(ride => ride.UpdatedAt).IsRequired();
    }
}
