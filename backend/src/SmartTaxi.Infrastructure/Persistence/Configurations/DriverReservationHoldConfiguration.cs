using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class DriverReservationHoldConfiguration : IEntityTypeConfiguration<DriverReservationHold>
{
    public void Configure(EntityTypeBuilder<DriverReservationHold> builder)
    {
        builder.ToTable("DriverReservationHolds");

        builder.HasKey(hold => hold.Id);
        builder.Property(hold => hold.Id).ValueGeneratedNever();

        builder.Property(hold => hold.RideId).IsRequired();
        builder.HasIndex(hold => hold.RideId);

        builder.Property(hold => hold.DriverId).IsRequired();
        builder.Property(hold => hold.VehicleId).IsRequired();

        builder.Property(hold => hold.HeldAt).IsRequired();
        builder.Property(hold => hold.ExpiresAt).IsRequired();
        builder.HasIndex(hold => hold.ExpiresAt);

        builder.Property(hold => hold.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The core "two Customers can never reserve the same Driver" guarantee:
        // Postgres itself rejects a second Active hold for the same Driver.
        builder.HasIndex(hold => hold.DriverId).IsUnique().HasFilter("\"Status\" = 'Active'");
    }
}
