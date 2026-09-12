using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Maintenance.Repositories;

/// <summary>
/// Mandatory atomicity design (Module 9 approved plan correction). One
/// transaction: MaintenanceRequest -> Completed, the immutable
/// MaintenanceRecord insert, the authoritative FleetExpense insert (via
/// Fleet's own IFleetExpenseRepository — never a second expense system), and
/// a best-effort Fleet release. The release's result is deliberately NOT used
/// to fail the transaction — see the explicit business rule on
/// IMaintenanceCompletionRepository's own doc comment: a vehicle that's no
/// longer UnderMaintenance for a legitimate independent reason (e.g.
/// Suspended) must never be silently reactivated, and completion must still
/// succeed regardless, because the maintenance work itself genuinely
/// finished.
/// </summary>
internal sealed class MaintenanceCompletionRepository : IMaintenanceCompletionRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses =
    [
        MaintenanceRequestStatus.InProgress, MaintenanceRequestStatus.WaitingForParts
    ];

    private readonly ApplicationDbContext _context;
    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly IFleetExpenseRepository _fleetExpenseRepository;

    public MaintenanceCompletionRepository(
        ApplicationDbContext context, IMaintenanceRequestRepository requestRepository, IVehicleRepository vehicleRepository,
        IFleetExpenseRepository fleetExpenseRepository)
    {
        _context = context;
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
        _fleetExpenseRepository = fleetExpenseRepository;
    }

    public async Task<MaintenanceCompletionResult> TryCompleteAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, MaintenanceRecord record, FleetExpense expense, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.Completed, requiredGarageUserId: garageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: record.FinalCost, reason: null, cancelledByUserId: null, utcNow,
            cancellationToken);

        if (!requestTransitioned)
        {
            await transaction.RollbackAsync(cancellationToken);
            return MaintenanceCompletionResult.RequestNotEligible;
        }

        await _context.MaintenanceRecords.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _fleetExpenseRepository.AddAsync(expense, cancellationToken);

        // Best-effort, unconditionally attempted, never gates the transaction — see the explicit business
        // rule in this method's doc comment. A `false` result means the vehicle was already not
        // UnderMaintenance for another legitimate reason, which is the correct outcome, not an error.
        await _vehicleRepository.TryReleaseFromMaintenanceAsync(vehicleId, utcNow, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return MaintenanceCompletionResult.Completed;
    }
}
