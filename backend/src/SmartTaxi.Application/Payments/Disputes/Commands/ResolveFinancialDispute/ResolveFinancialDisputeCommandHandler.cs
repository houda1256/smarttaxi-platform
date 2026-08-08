using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;

namespace SmartTaxi.Application.Payments.Disputes.Commands.ResolveFinancialDispute;

public sealed class ResolveFinancialDisputeCommandHandler : ICommandHandler<ResolveFinancialDisputeCommand, Result>
{
    private const string NotFoundError = "Litige financier introuvable.";
    private const string NotResolvableError = "Ce litige ne peut plus être résolu.";
    private const string ResolutionRequiredError = "Une résolution est requise.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public ResolveFinancialDisputeCommandHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result> Handle(ResolveFinancialDisputeCommand command, CancellationToken cancellationToken)
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

        var resolved = await _disputeRepository.TryResolveAsync(
            command.DisputeId, command.Resolution, command.Outcome, command.ResolvedBy, DateTime.UtcNow, cancellationToken);

        return resolved ? Result.Success() : Result.Failure(NotResolvableError, ErrorType.Conflict);
    }
}
