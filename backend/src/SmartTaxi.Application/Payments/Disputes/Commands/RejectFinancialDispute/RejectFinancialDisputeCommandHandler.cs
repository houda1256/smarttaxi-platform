using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;

namespace SmartTaxi.Application.Payments.Disputes.Commands.RejectFinancialDispute;

public sealed class RejectFinancialDisputeCommandHandler : ICommandHandler<RejectFinancialDisputeCommand, Result>
{
    private const string NotFoundError = "Litige financier introuvable.";
    private const string NotRejectableError = "Ce litige ne peut plus être rejeté.";
    private const string ResolutionRequiredError = "Un motif de rejet est requis.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public RejectFinancialDisputeCommandHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result> Handle(RejectFinancialDisputeCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Resolution))
        {
            return Result.Failure(ResolutionRequiredError, ErrorType.Validation);
        }

        var dispute = await _disputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);

        if (dispute is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var rejected = await _disputeRepository.TryRejectAsync(command.DisputeId, command.Resolution, command.ResolvedBy, DateTime.UtcNow, cancellationToken);

        return rejected ? Result.Success() : Result.Failure(NotRejectableError, ErrorType.Conflict);
    }
}
