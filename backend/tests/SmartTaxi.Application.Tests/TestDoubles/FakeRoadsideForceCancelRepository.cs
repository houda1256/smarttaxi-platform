using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Policies;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRoadsideForceCancelRepository : IRoadsideForceCancelRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses =
        Enum.GetValues<RoadsideRequestStatus>()
            .Except([
                RoadsideRequestStatus.Requested, RoadsideRequestStatus.Completed, RoadsideRequestStatus.Cancelled,
                RoadsideRequestStatus.Expired, RoadsideRequestStatus.Disputed
            ])
            .ToArray();

    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;

    public FakeRoadsideForceCancelRepository(FakeRoadsideAssistanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<bool> TryForceCancelAsync(Guid requestId, Guid adminUserId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);

        if (request is null)
        {
            return false;
        }

        var wasInProgressAndImmobilizing = request.Status == RoadsideRequestStatus.InProgress
            && RoadsideServiceTypePolicy.RequiresVehicleImmobilization(request.ServiceType);

        var transitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.Cancelled, requiredRequesterUserId: null, requiredPartnerUserId: null,
            finalCost: null, reason, cancelledByUserId: adminUserId, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!transitioned)
        {
            return false;
        }

        if (wasInProgressAndImmobilizing)
        {
            await _vehicleRepository.TryReleaseFromRoadsideAssistanceAsync(request.VehicleId, utcNow, cancellationToken);
        }

        return true;
    }
}
