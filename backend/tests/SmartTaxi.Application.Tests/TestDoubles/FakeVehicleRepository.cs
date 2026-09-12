using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeVehicleRepository : IVehicleRepository
{
    private readonly Dictionary<Guid, Vehicle> _vehiclesById = new();

    public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        _vehiclesById[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    public Task<Vehicle?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        Task.FromResult(_vehiclesById.GetValueOrDefault(vehicleId));

    public Task<IReadOnlyCollection<Vehicle>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Vehicle> vehicles = _vehiclesById.Values.Where(v => v.OwnerId == ownerId).ToList();
        return Task.FromResult(vehicles);
    }

    public Task<IReadOnlyCollection<Vehicle>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Vehicle> vehicles = _vehiclesById.Values.Where(v => v.FleetId == fleetId).ToList();
        return Task.FromResult(vehicles);
    }

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        _vehiclesById[vehicle.Id] = vehicle;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithLicensePlateAsync(string licensePlate, Guid? excludeVehicleId, CancellationToken cancellationToken)
    {
        var exists = _vehiclesById.Values.Any(v =>
            v.LicensePlate == licensePlate && v.Id != excludeVehicleId);
        return Task.FromResult(exists);
    }

    public Task<bool> ExistsWithVinAsync(string vin, Guid? excludeVehicleId, CancellationToken cancellationToken)
    {
        var exists = _vehiclesById.Values.Any(v => v.Vin == vin && v.Id != excludeVehicleId);
        return Task.FromResult(exists);
    }

    public Task<bool> TryApproveAsync(Guid vehicleId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.PendingVerification)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.VerificationStatus), VehicleVerificationStatus.Approved);
        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Active);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(Guid vehicleId, Guid rejectedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.PendingVerification)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.VerificationStatus), VehicleVerificationStatus.Rejected);
        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Rejected);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TrySuspendAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.Active)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Suspended);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRetireAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus == VehicleOperationalStatus.Retired)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Retired);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkUnderMaintenanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.Active)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.UnderMaintenance);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReleaseFromMaintenanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.UnderMaintenance)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Active);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkUnderRoadsideAssistanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.Active)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.UnderRoadsideAssistance);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReleaseFromRoadsideAssistanceAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_vehiclesById.TryGetValue(vehicleId, out var vehicle) || vehicle.OperationalStatus != VehicleOperationalStatus.UnderRoadsideAssistance)
        {
            return Task.FromResult(false);
        }

        SetProperty(vehicle, nameof(Vehicle.OperationalStatus), VehicleOperationalStatus.Active);
        SetProperty(vehicle, nameof(Vehicle.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(Vehicle vehicle, string propertyName, object? value) =>
        typeof(Vehicle).GetProperty(propertyName)!.SetValue(vehicle, value);
}
