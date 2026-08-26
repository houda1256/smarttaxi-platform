using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Notifications.Enums;
using SmartTaxi.Domain.Payments;
using SmartTaxi.Domain.Payments.Entities;
using SmartTaxi.Domain.Payments.Enums;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Payments.Commands.ConfirmPayment;

/// <summary>
/// Idempotent by design: a second confirm call on an already-Paid Payment is
/// a harmless no-op success (never a double revenue distribution), and a
/// call that loses the atomic race to another confirmer is re-checked the
/// same way rather than treated as an error — the "idempotent payment
/// confirmation" and "concurrency protection" requirements are the same
/// mechanism here. Closing the Ride (AwaitingPayment -> Completed) is
/// best-effort: the money side of this operation is authoritative regardless
/// of whether the Ride's own status has moved on for an unrelated reason.
/// </summary>
public sealed class ConfirmPaymentCommandHandler : ICommandHandler<ConfirmPaymentCommand, Result<decimal>>
{
    private const string NotFoundError = "Paiement introuvable.";
    private const string NotParticipantError = "Seuls les participants au paiement peuvent le confirmer.";
    private const string NotConfirmableError = "Ce paiement ne peut plus être confirmé.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IRevenueSharingCalculator _revenueSharingCalculator;
    private readonly IRideRepository _rideRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IInvoicePdfGenerator _invoicePdfGenerator;
    private readonly IReceiptPdfGenerator _receiptPdfGenerator;
    private readonly IInvoiceTaxPolicy _taxPolicy;
    private readonly ILedgerPostingService _ledgerPostingService;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILoyaltyEarningDispatcher _loyaltyEarningDispatcher;

    public ConfirmPaymentCommandHandler(
        IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository,
        IRevenueSharingCalculator revenueSharingCalculator, IRideRepository rideRepository,
        IInvoiceRepository invoiceRepository, IReceiptRepository receiptRepository, IInvoicePdfGenerator invoicePdfGenerator,
        IReceiptPdfGenerator receiptPdfGenerator, IInvoiceTaxPolicy taxPolicy, ILedgerPostingService ledgerPostingService,
        INotificationDispatcher notificationDispatcher, ILoyaltyEarningDispatcher loyaltyEarningDispatcher)
    {
        _paymentRepository = paymentRepository;
        _driverRepository = driverRepository;
        _revenueSharingCalculator = revenueSharingCalculator;
        _rideRepository = rideRepository;
        _invoiceRepository = invoiceRepository;
        _receiptRepository = receiptRepository;
        _invoicePdfGenerator = invoicePdfGenerator;
        _receiptPdfGenerator = receiptPdfGenerator;
        _taxPolicy = taxPolicy;
        _ledgerPostingService = ledgerPostingService;
        _notificationDispatcher = notificationDispatcher;
        _loyaltyEarningDispatcher = loyaltyEarningDispatcher;
    }

    public async Task<Result<decimal>> Handle(ConfirmPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(payment.DriverId, cancellationToken);

        if (driver is null)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (command.RequestingUserId != payment.CustomerId && driver.UserId != command.RequestingUserId)
        {
            return Result<decimal>.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        if (payment.Status == PaymentStatus.Paid)
        {
            return Result<decimal>.Success(payment.FinalFareAmount);
        }

        if (PaymentStatusTransitions.IsTerminal(payment.Status) || payment.Status == PaymentStatus.PartiallyRefunded)
        {
            return Result<decimal>.Failure(NotConfirmableError, ErrorType.Conflict);
        }

        var split = await _revenueSharingCalculator.CalculateAsync(payment.DriverId, payment.OwnerId, payment.FinalFareAmount, cancellationToken);
        var utcNow = DateTime.UtcNow;

        var confirmed = await _paymentRepository.TryConfirmAsync(
            payment.Id, split.PlatformCommissionAmount, split.DriverAmount, split.OwnerAmount, utcNow, cancellationToken);

        if (!confirmed)
        {
            var reloaded = await _paymentRepository.GetByIdAsync(payment.Id, cancellationToken);

            return reloaded?.Status == PaymentStatus.Paid
                ? Result<decimal>.Success(reloaded.FinalFareAmount)
                : Result<decimal>.Failure(NotConfirmableError, ErrorType.Conflict);
        }

        await _rideRepository.TryTransitionAsync(
            payment.RideId, RideStatus.AwaitingPayment, RideStatus.Completed, command.RequestingUserId, null, utcNow, cancellationToken);

        await GenerateInvoiceAndReceiptAsync(payment.Id, payment.RideNumber, payment.CustomerId, payment.DriverId, payment.OwnerId,
            payment.FinalFareAmount, payment.Currency, payment.PaymentMethod, payment.PaymentReference, utcNow, cancellationToken);

        await _ledgerPostingService.PostPaymentConfirmedAsync(
            payment.Id, driver.UserId, payment.OwnerId, payment.FinalFareAmount, split.PlatformCommissionAmount,
            split.DriverAmount, split.OwnerAmount, payment.Currency, command.RequestingUserId, utcNow, cancellationToken);

        // Financial confirmation is mandatory/never-disableable — see INotificationPreferencePolicy.
        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                payment.CustomerId, NotificationCategory.Payment, "payment.confirmed",
                new Dictionary<string, string> { ["Amount"] = payment.FinalFareAmount.ToString("F2"), ["Currency"] = payment.Currency },
                IsMandatory: true, SourceType: "Payment", SourceId: payment.Id),
            cancellationToken);

        // The one safe Loyalty earning trigger (see the Module 7 audit) — never Ride completion, only a
        // confirmed Payment. Both the payer and the driver are eligible actors for the same paid ride;
        // each award is independently idempotent (the ledger's idempotency key includes UserId), so
        // dispatching for a role with no configured earning rule is a harmless no-op.
        await _loyaltyEarningDispatcher.AwardForPaymentAsync(
            new LoyaltyPaymentAwardRequest(payment.CustomerId, UserRole.Customer, payment.Id, payment.FinalFareAmount), cancellationToken);
        await _loyaltyEarningDispatcher.AwardForPaymentAsync(
            new LoyaltyPaymentAwardRequest(driver.UserId, UserRole.Driver, payment.Id, payment.FinalFareAmount), cancellationToken);

        return Result<decimal>.Success(payment.FinalFareAmount);
    }

    /// <summary>Only ever reached once per Payment — both idempotent-replay branches above return before this point, so Invoice/Receipt are never duplicated.</summary>
    private async Task GenerateInvoiceAndReceiptAsync(
        Guid paymentId, string rideNumber, Guid customerId, Guid driverId, Guid ownerId, decimal totalAmount, string currency,
        PaymentMethod paymentMethod, string paymentReference, DateTime utcNow, CancellationToken cancellationToken)
    {
        var subtotal = Math.Round(totalAmount / (1 + _taxPolicy.TaxPercentage / 100m), 2);
        var taxAmount = totalAmount - subtotal;

        var invoice = Invoice.Generate(paymentId, rideNumber, customerId, driverId, ownerId, subtotal, taxAmount, currency, paymentMethod, utcNow);
        await _invoiceRepository.AddAsync(invoice, cancellationToken);

        var invoicePdfKey = await _invoicePdfGenerator.GenerateAsync(
            new InvoicePdfData(invoice.InvoiceNumber, rideNumber, customerId, driverId, ownerId, subtotal, taxAmount, totalAmount, currency, paymentMethod, utcNow),
            cancellationToken);
        await _invoiceRepository.TryAttachPdfAsync(invoice.Id, invoicePdfKey, cancellationToken);

        var receipt = Receipt.Issue(paymentId, totalAmount, currency, utcNow);
        await _receiptRepository.AddAsync(receipt, cancellationToken);

        var receiptPdfKey = await _receiptPdfGenerator.GenerateAsync(
            new ReceiptPdfData(receipt.ReceiptNumber, paymentReference, totalAmount, currency, utcNow), cancellationToken);
        await _receiptRepository.TryAttachPdfAsync(receipt.Id, receiptPdfKey, cancellationToken);
    }
}
