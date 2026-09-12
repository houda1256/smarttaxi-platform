using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.DisputeRoadsideAssistanceRequest;

/// <summary>
/// Admin-controlled flag only — Completed -&gt; Disputed freezes settlement
/// (SettleRoadsideAssistanceRequestCommandHandler refuses once Status is no
/// longer Completed). No dispute-resolution workflow exists in Module 10 —
/// this is a terminal marker, matching the approved plan's explicit scope cut.
/// </summary>
public sealed class DisputeRoadsideAssistanceRequestCommandHandler : ICommandHandler<DisputeRoadsideAssistanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Seule une demande complétée et non réglée peut être mise en litige.";

    private static readonly RoadsideRequestStatus[] AllowedFromStatuses = [RoadsideRequestStatus.Completed];

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;

    public DisputeRoadsideAssistanceRequestCommandHandler(IRoadsideAssistanceRequestRepository requestRepository)
    {
        _requestRepository = requestRepository;
    }

    public async Task<Result> Handle(DisputeRoadsideAssistanceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.SettledAtUtc is not null)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        var transitioned = await _requestRepository.TryTransitionAsync(
            request.Id, AllowedFromStatuses, RoadsideRequestStatus.Disputed, requiredRequesterUserId: null, requiredPartnerUserId: null,
            finalCost: null, reason: null, cancelledByUserId: null, clearSelectedPartner: false, DateTime.UtcNow, cancellationToken);

        return transitioned ? Result.Success() : Result.Failure(NotEligibleError, ErrorType.Conflict);
    }
}
