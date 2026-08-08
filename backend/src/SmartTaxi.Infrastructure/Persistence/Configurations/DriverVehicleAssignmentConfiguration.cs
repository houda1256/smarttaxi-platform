using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTaxi.Domain.Fleet.Assignments.Entities;

namespace SmartTaxi.Infrastructure.Persistence.Configurations;

public sealed class DriverVehicleAssignmentConfiguration : IEntityTypeConfiguration<DriverVehicleAssignment>
{
    public void Configure(EntityTypeBuilder<DriverVehicleAssignment> builder)
    {
        builder.ToTable("DriverVehicleAssignments");

        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).ValueGeneratedNever();

        builder.Property(assignment => assignment.DriverId).IsRequired();
        builder.HasIndex(assignment => assignment.DriverId);

        builder.Property(assignment => assignment.VehicleId).IsRequired();
        builder.HasIndex(assignment => assignment.VehicleId);

        builder.Property(assignment => assignment.OwnerId).IsRequired();
        builder.HasIndex(assignment => assignment.OwnerId);

        builder.Property(assignment => assignment.StartDate).IsRequired();
        builder.Property(assignment => assignment.EndDate);
        builder.Property(assignment => assignment.StartTime);
        builder.Property(assignment => assignment.EndTime);

        // [Flags] enum persisted via HasConversion<string>() — EF/Enum.Parse round-trips
        // comma-separated flag names (e.g. "Monday, Wednesday") without a custom converter.
        builder.Property(assignment => assignment.DaysOfWeek).HasConversion<string>().HasMaxLength(100).IsRequired();

        builder.Property(assignment => assignment.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(assignment => assignment.AssignedBy).IsRequired();

        builder.Property(assignment => assignment.CreatedAt).IsRequired();
        builder.Property(assignment => assignment.UpdatedAt).IsRequired();

        builder.HasIndex(assignment => assignment.Status);
    }
}
