using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Application.Payments.Disputes.Commands.OpenFinancialDispute;

/// <summary>Reserving the disputed amount happens atomically with the insert in IFinancialDisputeRepository.TryOpenAsync — a dispute is never left in a state where it exists but its funds aren't actually held, or vice versa.</summary>
public sealed class OpenFinancialDisputeCommandHandler : ICommandHandler<OpenFinancialDisputeCommand, Result<Guid>>
{
    private const string InsufficientBalanceError = "Impossible d'ouvrir ce litige : référence introuvable ou solde disponible insuffisant pour réserver ce montant.";

    private readonly IFinancialDisputeRepository _disputeRepository;

    public OpenFinancialDisputeCommandHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public async Task<Result<Guid>> Handle(OpenFinancialDisputeCommand command, CancellationToken cancellationToken)
    {
        FinancialDispute dispute;

        try
        {
            dispute = FinancialDispute.Open(
                command.Category, command.RelatedPaymentId, command.RelatedInvoiceId, command.RelatedPayoutId,
                command.DisputedAmount, command.Currency, command.Description, command.EvidenceReference, command.RaisedBy, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var opened = await _disputeRepository.TryOpenAsync(dispute, command.RaisedBy, DateTime.UtcNow, cancellationToken);

        return opened ? Result<Guid>.Success(dispute.Id) : Result<Guid>.Failure(InsufficientBalanceError, ErrorType.Conflict);
    }
}
