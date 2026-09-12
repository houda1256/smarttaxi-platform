using SmartTaxi.Application.Fleet.Expenses.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Domain.Fleet.Expenses.Entities;
using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of MaintenanceCompletionRepository's cross-aggregate atomicity — composes the SAME fakes a test also holds references to, so effects (or their absence, on RequestNotEligible) are genuinely visible everywhere.</summary>
public sealed class FakeMaintenanceCompletionRepository : IMaintenanceCompletionRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses =
    [
        MaintenanceRequestStatus.InProgress, MaintenanceRequestStatus.WaitingForParts
    ];

    private readonly FakeMaintenanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;
    private readonly FakeMaintenanceRecordRepository _recordRepository;
    private readonly IFleetExpenseRepository _fleetExpenseRepository;

    public bool ThrowOnFleetExpenseInsert { get; set; }

    public FakeMaintenanceCompletionRepository(
        FakeMaintenanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository,
        FakeMaintenanceRecordRepository recordRepository, IFleetExpenseRepository fleetExpenseRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
        _recordRepository = recordRepository;
        _fleetExpenseRepository = fleetExpenseRepository;
    }

    public async Task<MaintenanceCompletionResult> TryCompleteAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, MaintenanceRecord record, FleetExpense expense, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (ThrowOnFleetExpenseInsert)
        {
            // Simulates the whole transaction rolling back on a genuine DB exception — nothing committed,
            // same observable end-state as a real Postgres ROLLBACK, without needing this fake to implement
            // undo logic for the request transition it would otherwise have already performed.
            throw new InvalidOperationException("Simulated FleetExpense insert failure (test).");
        }

        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.Completed, requiredGarageUserId: garageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: record.FinalCost, reason: null, cancelledByUserId: null, utcNow,
            cancellationToken);

        if (!requestTransitioned)
        {
            return MaintenanceCompletionResult.RequestNotEligible;
        }

        _recordRepository.Add(record);
        await _fleetExpenseRepository.AddAsync(expense, cancellationToken);

        // Best-effort, unconditionally attempted, never gates the outcome — same non-gating semantics as
        // the real Infrastructure implementation.
        await _vehicleRepository.TryReleaseFromMaintenanceAsync(vehicleId, utcNow, cancellationToken);

        return MaintenanceCompletionResult.Completed;
    }
}
