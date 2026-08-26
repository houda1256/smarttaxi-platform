using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.SettleMaintenanceRequest;

/// <summary>
/// Exactly the Module 8 audit's hardened A–G settlement-recovery design,
/// applied from day one: check SettledAtUtc first; attempt posting; on
/// success mark settled; if posting reports the deterministic source already
/// exists, verify via the existing Finance abstraction whether the expected
/// (SourceType=MaintenanceRequest, SourceId=RequestId,
/// EntryType=MaintenanceRevenue) entry actually exists; if so, treat as
/// recovery and mark settled; never post a second ledger entry; never
/// interpret an unrelated ledger entry as successful settlement.
/// </summary>
public sealed class SettleMaintenanceRequestCommandHandler : ICommandHandler<SettleMaintenanceRequestCommand, Result>
{
    private const string NotFoundError = "Demande de maintenance introuvable.";
    private const string NotEligibleError = "Cette demande n'est pas éligible à la facturation dans son état actuel.";
    private const string NothingToSettleError = "Aucun coût final à régler.";
    private const string AlreadySettledError = "Cette demande a déjà été réglée.";
    private const string Currency = "TND";

    private readonly IMaintenanceRequestRepository _requestRepository;
    private readonly IMaintenanceBillingService _billingService;

    public SettleMaintenanceRequestCommandHandler(IMaintenanceRequestRepository requestRepository, IMaintenanceBillingService billingService)
    {
        _requestRepository = requestRepository;
        _billingService = billingService;
    }

    public async Task<Result> Handle(SettleMaintenanceRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _requestRepository.GetByIdAsync(command.RequestId, cancellationToken);

        if (request is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (request.Status != MaintenanceRequestStatus.Completed)
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

        var utcNow = DateTime.UtcNow;

        // B/C. Normal path: attempt the ledger posting, then mark settled.
        var posted = await _billingService.TrySettleRequestAsync(
            request.Id, request.OwnerUserId, request.GarageUserId, request.FinalCost.Value, Currency, utcNow, cancellationToken);

        if (posted)
        {
            await _requestRepository.TryMarkSettledAsync(request.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        // D/E. The deterministic (SourceType="MaintenanceRequest", SourceId=requestId) source already
        // exists — verify the EXPECTED entry (EntryType=MaintenanceRevenue) is really there before treating
        // this as recovery, never a second posting (F) and never a false-positive from an unrelated entry (G).
        if (await _billingService.HasSettlementEntryAsync(request.Id, cancellationToken))
        {
            await _requestRepository.TryMarkSettledAsync(request.Id, utcNow, cancellationToken);
            return Result.Success();
        }

        return Result.Failure(AlreadySettledError, ErrorType.Conflict);
    }
}
