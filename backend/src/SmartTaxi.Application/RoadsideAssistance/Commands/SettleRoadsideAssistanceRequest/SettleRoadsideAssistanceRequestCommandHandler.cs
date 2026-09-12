using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.RoadsideAssistance.Abstractions;
using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Application.RoadsideAssistance.Commands.SettleRoadsideAssistanceRequest;

/// <summary>
/// Exactly the Module 8/9 hardened A–G settlement-recovery design, applied
/// from day one: check SettledAtUtc first; attempt posting; on success mark
/// settled; if posting reports the deterministic source already exists,
/// verify via the existing Finance abstraction whether the expected
/// (SourceType=RoadsideAssistanceRequest, SourceId=RequestId,
/// EntryType=RoadsideAssistanceRevenue) entry actually exists; if so, treat
/// as recovery and mark settled; never post a second ledger entry; never
/// interpret an unrelated ledger entry as successful settlement. Disputed
/// requests are never settled, per the approved plan.
/// </summary>
public sealed class SettleRoadsideAssistanceRequestCommandHandler : ICommandHandler<SettleRoadsideAssistanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande d'assistance routière introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas éligible au règlement dans son état actuel.";
    private const string NothingToSettleError = "Aucun coût final à régler.";
    private const string AlreadySettledError = "Cette demande a déjà été réglée.";
    private const string Currency = "TND";

    private readonly IRoadsideAssistanceRequestRepository _requestRepository;
    private readonly IRoadsideAssistanceBillingService _billingService;

    public SettleRoadsideAssistanceRequestCommandHandler(
        IRoadsideAssistanceRequestRepository requestRepository, IRoadsideAssistanceBillingService billingService)
    {
        _requestRepository = requestRepository;
        _billingService = billingService;
    }

    public async Task<Result> Handle(SettleRoadsideAssistanceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != RoadsideRequestStatus.Completed)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        // A. Already settled — unchanged API semantics, no ledger/recovery work needed.
        if (request.SettledAtUtc is not null)
        {
            return Result.Failure(AlreadySettledError, ErrorType.Conflict);
        }

        if (request.FinalCost is null or <= 0)
        {
            return Result.Failure(NothingToSettleError, ErrorType.Validation);
        }

        if (request.SelectedPartnerUserId is not { } partnerUserId)
        {
            return Result.Failure(NotEligibleError, ErrorType.Conflict);
        }

        var utcNow = DateTime.UtcNow;

        // B/C. Normal path: attempt the ledger posting, then mark settled.
        var posted = await _billingService.TrySettleRequestAsync(
            request.Id, request.RequesterUserId, request.RequesterRole, partnerUserId, request.FinalCost.Value, Currency, utcNow,
            cancellationToken);

        if (posted)
        {
            await _requestRepository.TryMarkSettledAsync(request.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        // D/E. The deterministic (SourceType="RoadsideAssistanceRequest", SourceId=requestId) source already
        // exists — verify the EXPECTED entry (EntryType=RoadsideAssistanceRevenue) is really there before
        // treating this as recovery, never a second posting (F) and never a false-positive from an unrelated entry (G).
        if (await _billingService.HasSettlementEntryAsync(request.Id, cancellationToken))
        {
            await _requestRepository.TryMarkSettledAsync(request.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        return Result.Failure(AlreadySettledError, ErrorType.Conflict);
    }
}
