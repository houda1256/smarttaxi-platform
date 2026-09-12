using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.ReselectRoadsideRequest;

/// <summary>
/// Explicit requester action, Rejected -&gt; PartnersAvailable (approved plan,
/// Q1) — never automatic. Clears SelectedPartnerUserId only; the previous
/// cycle's RoadsidePartnerSelectionHistory row is left untouched (its
/// Response/RejectionReason/RespondedAtUtc are never erased), and a new cycle
/// begins the next time SelectRoadsidePartnerCommand is called.
/// </summary>
public sealed class ReselectRoadsideRequestCommandHandler : ICommandHandler<ReselectRoadsideRequestCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'a pas été refusée par le partenaire sélectionné.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.Rejected];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;

    public ReselectRoadsideRequestCommandHandler(IRoadsideAssistanceRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result> Handle(ReselectRoadsideRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, RoadsideRequestStatus.PartnersAvailable, requiredRequesterUserId: command.RequesterUserId,
            requiredPartnerUserId: null, finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: true, DateTime.UtcNow,
            cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}
