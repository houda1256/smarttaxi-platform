using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;

namespace SmartTaxi.Application.Payments.Commands.RefundPayment;

/// <summary>
/// Every refund creates an immutable RefundRecord row — the audit trail the
/// master prompt requires. TryApplyRefundAsync re-verifies both the status
/// and the cumulative refunded amount atomically, so two concurrent partial
/// refunds can never together exceed the Payment's final fare.
/// </summary>
public sealed class RefundPaymentCommandHandler : ICommandHandler<RefundPaymentCommand, Result<Guid>>
{
    private const string NotFoundError = "Paiement introuvable.";
    private const string NotRefundableError = "Ce paiement ne peut pas être remboursé.";
    private const string InvalidAmountError = "Le montant du remboursement partiel est requis et doit être positif.";
    private const string ExceedsRemainingError = "Le montant du remboursement dépasse le solde remboursable.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IRefundRecordRepository _refundRecordRepository;
    private readonly ILedgerPostingService _ledgerPostingService;

    public RefundPaymentCommandHandler(
        IPaymentRepository paymentRepository, IRefundRecordRepository refundRecordRepository, ILedgerPostingService ledgerPostingService)
    {
        _paymentRepository = paymentRepository;
        _refundRecordRepository = refundRecordRepository;
        _ledgerPostingService = ledgerPostingService;
    }

    public async Task<Result<Guid>> Handle(RefundPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (payment.Status is not (PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
        {
            return Result<Guid>.Failure(NotRefundableError, ErrorType.Conflict);
        }

        decimal refundAmount;

        if (command.RefundType == RefundType.Partial)
        {
            if (command.Amount is null || command.Amount <= 0)
            {
                return Result<Guid>.Failure(InvalidAmountError, ErrorType.Validation);
            }

            refundAmount = command.Amount.Value;
        }
        else
        {
            refundAmount = payment.RemainingRefundableAmount;
        }

        if (refundAmount > payment.RemainingRefundableAmount)
        {
            return Result<Guid>.Failure(ExceedsRemainingError, ErrorType.Validation);
        }

        var newRefundedTotal = payment.RefundedAmount + refundAmount;
        var newStatus = newRefundedTotal >= payment.FinalFareAmount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
        var utcNow = DateTime.UtcNow;

        var applied = await _paymentRepository.TryApplyRefundAsync(payment.Id, refundAmount, newStatus, utcNow, cancellationToken);

        if (!applied)
        {
            return Result<Guid>.Failure(NotRefundableError, ErrorType.Conflict);
        }

        RefundRecord record;

        try
        {
            record = RefundRecord.Issue(payment.Id, refundAmount, payment.Currency, command.RefundType, command.Reason, command.AdminUserId, utcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _refundRecordRepository.AddAsync(record, cancellationToken);

        await _ledgerPostingService.PostRefundAsync(record.Id, refundAmount, payment.Currency, command.AdminUserId, utcNow, cancellationToken);

        return Result<Guid>.Success(record.Id);
    }
}
