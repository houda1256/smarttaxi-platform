using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

/// <summary>
/// Admin escape hatch — allowed from any non-terminal status (see
/// MaintenanceRequestRepository.TerminalStatuses). One transaction: the
/// MaintenanceRequest -> Cancelled transition (no GarageUserId/OwnerUserId
/// requirement — this is an admin action), then a best-effort, unconditional
/// Fleet release attempt, same non-gating semantics as
/// MaintenanceCompletionRepository — a safe no-op when the vehicle was never
/// UnderMaintenance or already changed for another reason.
/// </summary>
internal sealed class MaintenanceForceCancelRepository : IMaintenanceForceCancelRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses =
        Enum.GetValues<MaintenanceRequestStatus>().Except(MaintenanceRequestRepository.TerminalStatuses).ToArray();

    private readonly ApplicationDbContext _context;
    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public MaintenanceForceCancelRepository(ApplicationDbContext context, IMaintenanceRequestRepository requestRepository, IVehicleRepository vehicleRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<bool> TryForceCancelAsync(
        Guid requestId, Guid adminUserId, Guid vehicleId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.Cancelled, requiredGarageUserId: null, requiredOwnerUserId: null,
            estimatedCost: null, finalCost: null, reason: reason, cancelledByUserId: adminUserId, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Best-effort, unconditionally attempted, never gates the transaction — safe no-op when the
        // vehicle was never UnderMaintenance (request was still in an early state) or already changed
        // for another reason.
        await _vehicleRepository.TryReleaseFromMaintenanceAsync(vehicleId, utcNow, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
