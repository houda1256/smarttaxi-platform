using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of RoadsideWorkStartRepository's cross-aggregate atomicity — composes the SAME FakeRoadsideAssistanceRequestRepository/FakeVehicleRepository instances a test also uses, so a rolled-back transition is genuinely visible to both.</summary>
public sealed class FakeRoadsideWorkStartRepository : IRoadsideWorkStartRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.PartnerArrived];

    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;

    public FakeRoadsideWorkStartRepository(FakeRoadsideAssistanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<RoadsideWorkStartResult> TryStartAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.InProgress, requiredRequesterUserId: null, requiredPartnerUserId: partnerUserId,
            finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            return RoadsideWorkStartResult.RequestNotEligible;
        }

        if (!await _vehicleRepository.TryMarkUnderRoadsideAssistanceAsync(vehicleId, utcNow, cancellationToken))
        {
            // Simulate rollback: revert the request transition performed above.
            await _requestRepository.TryTransitionAsync(
                requestId, [RoadsideRequestStatus.InProgress], RoadsideRequestStatus.PartnerArrived, requiredRequesterUserId: null,
                requiredPartnerUserId: partnerUserId, finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow,
                cancellationToken);

            return RoadsideWorkStartResult.VehicleNotEligible;
        }

        return RoadsideWorkStartResult.Started;
    }
}
