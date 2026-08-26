using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Application.RoadsideAssistance.Contracts;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of RoadsideCompletionRepository's cross-aggregate atomicity — the Fleet release is attempted unconditionally and never gates the outcome, same non-gating semantics as the real Infrastructure implementation.</summary>
public sealed class FakeRoadsideCompletionRepository : IRoadsideCompletionRepository
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.InProgress];

    private readonly FakeRoadsideAssistanceRequestRepository _requestRepository;
    private readonly FakeVehicleRepository _vehicleRepository;

    public FakeRoadsideCompletionRepository(FakeRoadsideAssistanceRequestRepository requestRepository, FakeVehicleRepository vehicleRepository)
    {
        _requestRepository = requestRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<RoadsideCompletionResult> TryCompleteAsync(
        Guid requestId, Guid partnerUserId, Guid vehicleId, decimal finalCost, DateTime utcNow, CancellationToken cancellationToken)
    {
        var requestTransitioned = await _requestRepository.TryTransitionAsync(
            requestId, AllowedFromStatuses, RoadsideRequestStatus.Completed, requiredRequesterUserId: null, requiredPartnerUserId: partnerUserId,
            finalCost, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow, cancellationToken);

        if (!requestTransitioned)
        {
            return RoadsideCompletionResult.RequestNotEligible;
        }

        // Best-effort, unconditionally attempted, never gates the outcome — same non-gating semantics as the real Infrastructure implementation.
        await _vehicleRepository.TryReleaseFromRoadsideAssistanceAsync(vehicleId, utcNow, cancellationToken);

        return RoadsideCompletionResult.Completed;
    }
}
