using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

/// <summary>
/// Mandatory atomicity design (Module 9 approved plan correction). Composes
/// IMaintenanceRequestRepository (Maintenance-side transition) and
/// IVehicleRepository (Fleet-side transition) — both injected with the SAME
/// ApplicationDbContext instance (standard DI-request scoping), so both
/// ExecuteUpdateAsync calls join the ambient transaction opened here and
/// commit or roll back together. No compensation logic: the rollback IS the
/// compensation, guaranteed by the DB transaction itself, exactly the design
/// correction requested — never two independently committed operations.
/// </summary>
internal sealed class MaintenanceWorkStartRepository : IMaintenanceWorkStartRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.VehicleReceived];

    private readonly ApplicationDbContext _context;
    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public MaintenanceWorkStartRepository(
        ApplicationDbContext context, IMaintenanceRequestRepository requestRepository, IVehicleRepository vehicleRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<MaintenanceWorkStartResult> TryStartAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.InProgress, requiredGarageUserId: garageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return MaintenanceWorkStartResult.RequestNotEligible;
        }

        var vehicleMarked = await _vehicleRepository.TryMarkUnderMaintenanceAsync(vehicleId, utcNow, cancellationToken);

        if (!vehicleMarked)
        {
            // Rolls back the MaintenanceRequest transition above too — same transaction, same connection.
            await transaction.RollbackAsync(cancellationToken);
            return MaintenanceWorkStartResult.VehicleNotEligible;
        }

        await transaction.CommitAsync(cancellationToken);
        return MaintenanceWorkStartResult.Started;
    }
}
