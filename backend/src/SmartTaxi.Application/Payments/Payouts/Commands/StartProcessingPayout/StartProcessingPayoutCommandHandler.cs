using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Commands.StartProcessingPayout;

public sealed class StartProcessingPayoutCommandHandler : ICommandHandler<StartProcessingPayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotProcessableError = "Ce versement ne peut pas démarrer son traitement.";

    private readonly IPayoutRepository _payoutRepository;

    public StartProcessingPayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(StartProcessingPayoutCommand command, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (payout.Status == PayoutStatus.Processing)
        {
            return Result.Success();
        }

        var started = await _payoutRepository.TryStartProcessingAsync(command.PayoutId, DateTime.UtcNow, cancellationToken);

        return started ? Result.Success() : Result.Failure(NotProcessableError, ErrorType.Conflict);
    }
}
