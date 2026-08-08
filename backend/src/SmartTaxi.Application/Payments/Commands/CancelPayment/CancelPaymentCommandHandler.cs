using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Commands.CancelPayment;

/// <summary>Only reachable before money has actually moved (Pending/Authorized) — once Paid, only a refund (an audited, distinct operation) can undo a Payment.</summary>
public sealed class CancelPaymentCommandHandler : ICommandHandler<CancelPaymentCommand, Result>
{
    private const string NotFoundError = "Paiement introuvable.";
    private const string NotParticipantError = "Seuls les participants au paiement peuvent l'annuler.";
    private const string NotCancellableError = "Ce paiement ne peut plus être annulé.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public CancelPaymentCommandHandler(IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository)
    {
        _paymentRepository = paymentRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(CancelPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);

        if (payment is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(payment.DriverId, cancellationToken);

        if (command.RequestingUserId != payment.CustomerId && (driver is null || driver.UserId != command.RequestingUserId))
        {
            return Result.Failure(NotParticipantError, ErrorType.Forbidden);
        }

        var cancelled = await _paymentRepository.TryCancelAsync(payment.Id, command.Reason, DateTime.UtcNow, cancellationToken);

        return cancelled ? Result.Success() : Result.Failure(NotCancellableError, ErrorType.Conflict);
    }
}
