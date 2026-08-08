using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicles");

        builder.HasKey(vehicle => vehicle.Id);
        builder.Property(vehicle => vehicle.Id).ValueGeneratedNever();

        builder.Property(vehicle => vehicle.OwnerId).IsRequired();
        builder.HasIndex(vehicle => vehicle.OwnerId);

        builder.Property(vehicle => vehicle.FleetId);
        builder.HasIndex(vehicle => vehicle.FleetId);

        builder.Property(vehicle => vehicle.Brand).HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.Model).HasMaxLength(100).IsRequired();
        builder.Property(vehicle => vehicle.Color).HasMaxLength(50).IsRequired();

        builder.Property(vehicle => vehicle.LicensePlate).HasMaxLength(20).IsRequired();
        builder.HasIndex(vehicle => vehicle.LicensePlate).IsUnique();

        builder.Property(vehicle => vehicle.Vin).HasMaxLength(50);
        builder.HasIndex(vehicle => vehicle.Vin).IsUnique();

        builder.Property(vehicle => vehicle.FuelType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(vehicle => vehicle.TransmissionType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(vehicle => vehicle.VehicleCategory).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(vehicle => vehicle.OperationalStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(vehicle => vehicle.VerificationStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(vehicle => vehicle.MainPhotoReference).HasMaxLength(500);

        builder.Property(vehicle => vehicle.CreatedAt).IsRequired();
        builder.Property(vehicle => vehicle.UpdatedAt).IsRequired();

        builder.HasIndex(vehicle => vehicle.OperationalStatus);
    }
}
