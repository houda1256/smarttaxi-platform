using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;

namespace SmartTaxi.Application.Payments.Payouts.Commands.RejectPayout;

public sealed class RejectPayoutCommandHandler : ICommandHandler<RejectPayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotRejectableError = "Ce versement ne peut plus être rejeté.";

    private readonly IPayoutRepository _payoutRepository;

    public RejectPayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(RejectPayoutCommand command, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var rejected = await _payoutRepository.TryRejectAsync(command.PayoutId, DateTime.UtcNow, cancellationToken);

        return rejected ? Result.Success() : Result.Failure(NotRejectableError, ErrorType.Conflict);
    }
}
