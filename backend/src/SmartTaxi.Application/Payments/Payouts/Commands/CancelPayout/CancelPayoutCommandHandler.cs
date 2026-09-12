using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;

namespace SmartTaxi.Application.Payments.Payouts.Commands.CancelPayout;

public sealed class CancelPayoutCommandHandler : ICommandHandler<CancelPayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotCancellableError = "Ce versement ne peut plus être annulé.";

    private readonly IPayoutRepository _payoutRepository;

    public CancelPayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(CancelPayoutCommand command, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var cancelled = await _payoutRepository.TryCancelAsync(command.PayoutId, DateTime.UtcNow, cancellationToken);

        return cancelled ? Result.Success() : Result.Failure(NotCancellableError, ErrorType.Conflict);
    }
}
