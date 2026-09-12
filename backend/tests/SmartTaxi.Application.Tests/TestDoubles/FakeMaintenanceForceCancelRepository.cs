using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeMaintenanceForceCancelRepository : IMaintenanceForceCancelRepository
{
    private static readonly MaintenanceRequestStatus[] AllowedFromStatuses =
        Enum.GetValues<MaintenanceRequestStatus>()
            .Except([
                MaintenanceRequestStatus.Completed, MaintenanceRequestStatus.Cancelled, MaintenanceRequestStatus.Rejected,
                MaintenanceRequestStatus.QuoteRejected
            ])
            .ToArray();

    private readonly FakeMaintenanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;

    public FakeMaintenanceForceCancelRepository(FakeMaintenanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<bool> TryForceCancelAsync(
        Guid requestId, Guid adminUserId, Guid vehicleId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, MaintenanceRequestStatus.Cancelled, requiredGarageUserId: null, requiredOwnerUserId: null,
            estimatedCost: null, finalCost: null, reason: reason, cancelledByUserId: adminUserId, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            return false;
        }

        await _vehicleRepository.TryReleaseFromMaintenanceAsync(vehicleId, utcNow, cancellationToken);
        return true;
    }
}
