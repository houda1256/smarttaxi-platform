using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ExpireStaleRoadsideRequests;

/// <summary>
/// Manual/admin-triggered sweep — no scheduler/background task exists in this
/// codebase (same convention as Fleet's expire-sweep and Subscriptions'
/// expire-due). Never touches Fleet: PartnersAvailable/PendingPartnerResponse/
/// Rejected never reach a state where the vehicle was marked
/// UnderRoadsideAssistance.
/// </summary>
public sealed class ExpireStaleRoadsideRequestsCommandHandler : ICommandHandler<ExpireStaleRoadsideRequestsCommand, Result<int>>
{
    private static readonly RoadsideRequestStatus[] AllowedFromStatuses =
    [
        RoadsideRequestStatus.PartnersAvailable, RoadsideRequestStatus.PendingPartnerResponse, RoadsideRequestStatus.Rejected
    ];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideExpiryPolicy _expiryPolicy;

    public ExpireStaleRoadsideRequestsCommandHandler(IRoadsideAssistanceRequestRepository requestRepository, IRoadsideExpiryPolicy expiryPolicy)
    {
        _requestRepository = requestRepository;
        _expiryPolicy = expiryPolicy;
    }

    public async Task<Result<int>> Handle(ExpireStaleRoadsideRequestsCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var staleBefore = utcNow - _expiryPolicy.StaleAfter;
        var stale = await _requestRepository.GetStaleForExpiryAsync(staleBefore, cancellationToken);

        var expiredCount = 0;

        foreach (var request in stale)
        {
            if (await _requestRepository.TryTransitionAsync(
                    request.Id, AllowedFromStatuses, RoadsideRequestStatus.Expired, requiredRequesterUserId: null,
                    requiredPartnerUserId: null, finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false, utcNow,
                    cancellationToken))
            {
                expiredCount++;
            }
        }

        return Result<int>.Success(expiredCount);
    }
}
