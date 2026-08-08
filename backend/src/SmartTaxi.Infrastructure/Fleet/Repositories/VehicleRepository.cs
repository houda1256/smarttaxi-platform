using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Vehicles.Entities;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class VehicleRepository : IVehicleRepository
{
    private readonly ApplicationDbContext _context;

    public VehicleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
    {
        await _context.Vehicles.AddAsync(vehicle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Vehicle?> GetByIdAsync(Guid vehicleId, CancellationToken cancellationToken) =>
        _context.Vehicles.FirstOrDefaultAsync(vehicle => vehicle.Id == vehicleId, cancellationToken);

    public async Task<IReadOnlyCollection<Vehicle>> GetForOwnerAsync(Guid ownerId, CancellationToken cancellationToken) =>
        await _context.Vehicles.Where(vehicle => vehicle.OwnerId == ownerId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Vehicle>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken) =>
        await _context.Vehicles.Where(vehicle => vehicle.FleetId == fleetId).ToListAsync(cancellationToken);

    public Task UpdateAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<bool> ExistsWithLicensePlateAsync(string licensePlate, Guid? excludeVehicleId, CancellationToken cancellationToken) =>
        _context.Vehicles.AnyAsync(
            vehicle => vehicle.LicensePlate == licensePlate && vehicle.Id != excludeVehicleId, cancellationToken);

    public Task<bool> ExistsWithVinAsync(string vin, Guid? excludeVehicleId, CancellationToken cancellationToken) =>
        _context.Vehicles.AnyAsync(vehicle => vehicle.Vin == vin && vehicle.Id != excludeVehicleId, cancellationToken);

    public async Task<bool> TryApproveAsync(Guid vehicleId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Vehicles
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.OperationalStatus == VehicleOperationalStatus.PendingVerification)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(vehicle => vehicle.VerificationStatus, VehicleVerificationStatus.Approved)
                .SetProperty(vehicle => vehicle.OperationalStatus, VehicleOperationalStatus.Active)
                .SetProperty(vehicle => vehicle.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRejectAsync(Guid vehicleId, Guid rejectedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Vehicles
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.OperationalStatus == VehicleOperationalStatus.PendingVerification)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(vehicle => vehicle.VerificationStatus, VehicleVerificationStatus.Rejected)
                .SetProperty(vehicle => vehicle.OperationalStatus, VehicleOperationalStatus.Rejected)
                .SetProperty(vehicle => vehicle.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TrySuspendAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Vehicles
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.OperationalStatus == VehicleOperationalStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(vehicle => vehicle.OperationalStatus, VehicleOperationalStatus.Suspended)
                .SetProperty(vehicle => vehicle.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryRetireAsync(Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Vehicles
            .Where(vehicle => vehicle.Id == vehicleId && vehicle.OperationalStatus != VehicleOperationalStatus.Retired)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(vehicle => vehicle.OperationalStatus, VehicleOperationalStatus.Retired)
                .SetProperty(vehicle => vehicle.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
