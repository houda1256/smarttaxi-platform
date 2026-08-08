using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Domain.Fleet.Vehicles.Events;

namespace SmartTaxi.Domain.Tests.Fleet.Vehicles.Entities;

public class VehicleTests
{
    private static Vehicle RegisterVehicle() => Vehicle.Register(
        Guid.NewGuid(), null, "Toyota", "Corolla", 2022, "White", "AA-123-BB", "VIN123", 1000,
        FuelType.Petrol, TransmissionType.Automatic, 5, true, false, VehicleCategory.Standard, null, DateTime.UtcNow);

    [Fact]
    public void Register_SetsPendingVerificationAndRaisesEvent()
    {
        var vehicle = RegisterVehicle();

        Assert.Equal(VehicleOperationalStatus.PendingVerification, vehicle.OperationalStatus);
        Assert.Equal(VehicleVerificationStatus.Pending, vehicle.VerificationStatus);
        Assert.False(vehicle.IsCurrentlyEligibleForRides());
        Assert.Single(vehicle.DomainEvents);
        Assert.IsType<VehicleRegistered>(vehicle.DomainEvents.Single());
    }

    [Fact]
    public void IsCurrentlyEligibleForRides_RequiresApprovedAndActive()
    {
        var vehicle = RegisterVehicle();
        typeof(Vehicle).GetProperty(nameof(Vehicle.VerificationStatus))!.SetValue(vehicle, VehicleVerificationStatus.Approved);
        typeof(Vehicle).GetProperty(nameof(Vehicle.OperationalStatus))!.SetValue(vehicle, VehicleOperationalStatus.Active);

        Assert.True(vehicle.IsCurrentlyEligibleForRides());
    }

    [Theory]
    [InlineData(VehicleOperationalStatus.UnderMaintenance)]
    [InlineData(VehicleOperationalStatus.Suspended)]
    [InlineData(VehicleOperationalStatus.Retired)]
    [InlineData(VehicleOperationalStatus.PendingVerification)]
    public void IsCurrentlyEligibleForRides_ForNonActiveOperationalStatus_ReturnsFalseEvenIfApproved(
        VehicleOperationalStatus status)
    {
        var vehicle = RegisterVehicle();
        typeof(Vehicle).GetProperty(nameof(Vehicle.VerificationStatus))!.SetValue(vehicle, VehicleVerificationStatus.Approved);
        typeof(Vehicle).GetProperty(nameof(Vehicle.OperationalStatus))!.SetValue(vehicle, status);

        Assert.False(vehicle.IsCurrentlyEligibleForRides());
    }

    [Fact]
    public void UpdateDetails_ReplacesEditableFields()
    {
        var vehicle = RegisterVehicle();

        vehicle.UpdateDetails(
            "Honda", "Civic", 2023, "Black", "CC-456-DD", "VIN999", 2000,
            FuelType.Diesel, TransmissionType.Manual, 4, false, true, VehicleCategory.Comfort, "photo-key", DateTime.UtcNow);

        Assert.Equal("Honda", vehicle.Brand);
        Assert.Equal("CC-456-DD", vehicle.LicensePlate);
        Assert.Equal(VehicleCategory.Comfort, vehicle.VehicleCategory);
    }
}
