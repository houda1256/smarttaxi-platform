using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Commands.ApprovePayout;

public sealed class ApprovePayoutCommandHandler : ICommandHandler<ApprovePayoutCommand, Result>
{
    private const string NotFoundError = "Versement introuvable.";
    private const string NotApprovableError = "Ce versement ne peut plus être approuvé.";

    private readonly IPayoutRepository _payoutRepository;

    public ApprovePayoutCommandHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result> Handle(ApprovePayoutCommand command, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(command.PayoutId, cancellationToken);

        if (payout is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (payout.Status == PayoutStatus.Approved)
        {
            return Result.Success();
        }

        var approved = await _payoutRepository.TryApproveAsync(command.PayoutId, command.ApprovedBy, DateTime.UtcNow, cancellationToken);

        return approved ? Result.Success() : Result.Failure(NotApprovableError, ErrorType.Conflict);
    }
}
