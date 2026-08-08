using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Contracts.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class DriverOwnerContractConfiguration : IEntityTypeConfiguration<DriverOwnerContract>
{
    public void Configure(EntityTypeBuilder<DriverOwnerContract> builder)
    {
        builder.ToTable("DriverOwnerContracts");

        builder.HasKey(contract => contract.Id);
        builder.Property(contract => contract.Id).ValueGeneratedNever();

        builder.Property(contract => contract.OwnerId).IsRequired();
        builder.Property(contract => contract.DriverId).IsRequired();
        builder.HasIndex(contract => contract.OwnerId);
        builder.HasIndex(contract => contract.DriverId);

        builder.Property(contract => contract.VehicleId);

        builder.Property(contract => contract.ContractType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(contract => contract.StartDate).IsRequired();
        builder.Property(contract => contract.EndDate);

        builder.Property(contract => contract.FixedAmount).HasPrecision(12, 2);
        builder.Property(contract => contract.DriverPercentage).HasPrecision(5, 2);
        builder.Property(contract => contract.OwnerPercentage).HasPrecision(5, 2);

        builder.Property(contract => contract.PaymentFrequency).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(contract => contract.DocumentReference).HasMaxLength(500);

        builder.Property(contract => contract.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(contract => contract.CreatedAt).IsRequired();
        builder.Property(contract => contract.UpdatedAt).IsRequired();

        // Closes the create/activate-time race for good: only one row per driver-owner
        // pair may be PendingSignature or Active at once, enforced by Postgres itself.
        builder.HasIndex(contract => new { contract.DriverId, contract.OwnerId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('PendingSignature', 'Active')");
    }
}
