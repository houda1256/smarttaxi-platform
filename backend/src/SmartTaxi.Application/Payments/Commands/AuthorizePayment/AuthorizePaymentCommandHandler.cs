using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Commands.AuthorizePayment;

/// <summary>Ceremonial intermediate step (mainly meaningful for Card, ahead of a future real gateway) — Cash/CashAtAgency payments can skip straight to Confirm.</summary>
public sealed class AuthorizePaymentCommandHandler : ICommandHandler<AuthorizePaymentCommand, Result>
{
    private const string NotFoundError = "Paiement introuvable.";
    private const string NotParticipantError = "Seuls les participants au paiement peuvent l'autoriser.";
    private const string NotPendingError = "Ce paiement n'est pas en attente.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IDriverProfileRepository _driverRepository;

    public AuthorizePaymentCommandHandler(IPaymentRepository paymentRepository, IDriverProfileRepository driverRepository)
    {
        _paymentRepository = paymentRepository;
        _driverRepository = driverRepository;
    }

    public async Task<Result> Handle(AuthorizePaymentCommand command, CancellationToken cancellationToken)
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

        var authorized = await _paymentRepository.TryAuthorizeAsync(payment.Id, DateTime.UtcNow, cancellationToken);

        return authorized ? Result.Success() : Result.Failure(NotPendingError, ErrorType.Conflict);
    }
}
