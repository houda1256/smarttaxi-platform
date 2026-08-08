using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Commands.CompletePayout;

/// <summary>
/// Idempotent by design, exactly like ConfirmPayment: an already-Paid Payout
/// is a harmless no-op success, never a second balance movement — the
/// "payout completion updates balances exactly once" requirement is enforced
/// by IPayoutRepository.TryCompleteAsync's own atomic guard, this early
/// return only covers the sequential-replay case.
/// </summary>
public sealed class CompletePayoutCommandHandler : ICommandHandler<CompletePayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotProcessingError = "Ce versement n'est pas en cours de traitement.";
    private const string InsufficientBalanceError = "Le solde disponible du bénéficiaire ne couvre plus ce versement.";

    private readonly IPayoutRepository _payoutRepository;

    public CompletePayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(CompletePayoutCommand command, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (payout.Status == PayoutStatus.Paid)
        {
            return Result.Success();
        }

        var outcome = await _payoutRepository.TryCompleteAsync(command.PayoutId, command.RequestingUserId, DateTime.UtcNow, cancellationToken);

        return outcome switch
        {
            PayoutCompletionOutcome.Completed => Result.Success(),
            PayoutCompletionOutcome.InsufficientAvailableBalance => Result.Failure(InsufficientBalanceError, ErrorType.Conflict),
            _ => Result.Failure(NotProcessingError, ErrorType.Conflict)
        };
    }
}
