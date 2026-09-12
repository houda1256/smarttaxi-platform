using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Drivers.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class DriverProfileConfiguration : IEntityTypeConfiguration<DriverProfile>
{
    public void Configure(EntityTypeBuilder<DriverProfile> builder)
    {
        builder.ToTable("DriverProfiles");

        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();

        builder.Property(profile => profile.UserId).IsRequired();
        builder.HasIndex(profile => profile.UserId).IsUnique();

        builder.Property(profile => profile.DriverLicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(profile => profile.DriverLicenseExpiration).IsRequired();
        builder.Property(profile => profile.TaxiLicenseNumber).HasMaxLength(50);

        builder.Property(profile => profile.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(profile => profile.AvailabilityStatus).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.Property(profile => profile.IndependentDriver).IsRequired();
        builder.Property(profile => profile.CurrentVehicleId);

        builder.Property(profile => profile.AverageRating).HasPrecision(3, 2).IsRequired();
        builder.Property(profile => profile.CompletedRideCount).IsRequired();
        builder.Property(profile => profile.CancellationCount).IsRequired();

        // Added for the Ride module's driver search/recommendation — a lightweight position
        // cache, not a tracked telemetry stream (see RideLocationPoint for that).
        builder.Property(profile => profile.LastKnownLatitude);
        builder.Property(profile => profile.LastKnownLongitude);
        builder.Property(profile => profile.LastLocationRecordedAt);

        builder.Property(profile => profile.CreatedAt).IsRequired();
        builder.Property(profile => profile.UpdatedAt).IsRequired();

        builder.HasIndex(profile => profile.VerificationStatus);
    }
}
