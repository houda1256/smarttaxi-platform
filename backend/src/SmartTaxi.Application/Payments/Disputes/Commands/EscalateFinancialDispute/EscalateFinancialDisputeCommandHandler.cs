using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;

namespace SmartTaxi.Application.Payments.Disputes.Commands.EscalateFinancialDispute;

public sealed class EscalateFinancialDisputeCommandHandler : ICommandHandler<EscalateFinancialDisputeCommand, Result>
{
    private const string NotFoundError = "Litige financier introuvable.";
    private const string NotEscalatableError = "Ce litige ne peut pas être escaladé.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public EscalateFinancialDisputeCommandHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result> Handle(EscalateFinancialDisputeCommand command, CancellationToken cancellationToken)
    {
        var dispute = await _disputeRepository.GetByIdAsync(command.DisputeId, cancellationToken);

        if (dispute is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var escalated = await _disputeRepository.TryEscalateAsync(command.DisputeId, DateTime.UtcNow, cancellationToken);

        return escalated ? Result.Success() : Result.Failure(NotEscalatableError, ErrorType.Conflict);
    }
}
