using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Maintenance.Contracts;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of MaintenanceWorkStartRepository's cross-aggregate atomicity — composes the SAME FakeMaintenanceRequestRepository/FakeVehicleRepository instances a test also uses, so a rolled-back transition is genuinely visible to both.</summary>
public sealed class FakeMaintenanceWorkStartRepository : IMaintenanceWorkStartRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses = [MaintenanceRequestStatus.VehicleReceived];

    private readonly FakeMaintenanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;

    public FakeMaintenanceWorkStartRepository(FakeMaintenanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<MaintenanceWorkStartResult> TryStartAsync(
        Guid requestId, Guid garageUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.InProgress, requiredGarageUserId: garageUserId,
            requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            return MaintenanceWorkStartResult.RequestNotEligible;
        }

        if (!await _vehicleRepository.TryMarkUnderMaintenanceAsync(vehicleId, utcNow, cancellationToken))
        {
            // Simulate rollback: revert the request transition performed above.
            await _requestRepository.TryTransitionAsync(
                requestId, [MaintenanceRequestStatus.InProgress], MaintenanceRequestStatus.VehicleReceived, requiredGarageUserId: garageUserId,
                requiredOwnerUserId: null, estimatedCost: null, finalCost: null, reason: null, cancelledByUserId: null, utcNow, cancellationToken);

            return MaintenanceWorkStartResult.VehicleNotEligible;
        }

        return MaintenanceWorkStartResult.Started;
    }
}
