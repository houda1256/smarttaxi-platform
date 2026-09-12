using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;

namespace SmartTaxi.Application.Payments.Disputes.Commands.StartReviewFinancialDispute;

public sealed class StartReviewFinancialDisputeCommandHandler : ICommandHandler<StartReviewFinancialDisputeCommand, Result>
{
    private const string NotFoundError = "Litige financier introuvable.";
    private const string NotReviewableError = "Ce litige ne peut pas être mis en revue.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public StartReviewFinancialDisputeCommandHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result> Handle(StartReviewFinancialDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);

        if (dispute is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var started = await _disputeRepository.TryStartReviewAsync(command.DisputeId, command.AssignedFinanceManagerId, DateTime.UtcNow, cancellationToken);

        return started ? Result.Success() : Result.Failure(NotReviewableError, ErrorType.Conflict);
    }
}
