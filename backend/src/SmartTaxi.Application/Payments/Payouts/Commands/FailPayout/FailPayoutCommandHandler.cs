using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;

namespace SmartTaxi.Application.Payments.Payouts.Commands.FailPayout;

public sealed class FailPayoutCommandHandler : ICommandHandler<FailPayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotFailableError = "Ce versement ne peut pas être marqué comme échoué.";
    private const string ReasonRequiredError = "Un motif est requis.";

    private readonly IPayoutRepository _payoutRepository;

    public FailPayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(FailPayoutCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var failed = await _payoutRepository.TryMarkFailedAsync(command.PayoutId, command.Reason, DateTime.UtcNow, cancellationToken);

        return failed ? Result.Success() : Result.Failure(NotFailableError, ErrorType.Conflict);
    }
}
