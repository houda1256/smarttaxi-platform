using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Payments.Commands.FailPayment;

public sealed class FailPaymentCommandHandler : ICommandHandler<FailPaymentCommand, Result>
{
    private const string NotFailableError = "Ce paiement ne peut plus être marqué comme échoué.";

    private readonly IPaymentRepository _paymentRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public FailPaymentCommandHandler(IPaymentRepository paymentRepository, INotificationDispatcher notificationDispatcher)
    {
        _paymentRepository = paymentRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(FailPaymentCommand command, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByIdAsync(command.PaymentId, cancellationToken);
        var failed = await _paymentRepository.TryFailAsync(command.PaymentId, command.Reason, DateTime.UtcNow, cancellationToken);

        if (!failed)
        {
            return Result.Failure(NotFailableError, ErrorType.Conflict);
        }

        if (payment is not null)
        {
            // Financial failure is mandatory/never-disableable — see INotificationPreferencePolicy.
            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    payment.CustomerId, NotificationCategory.Payment, "payment.failed",
                    new Dictionary<string, string> { ["Reason"] = command.Reason ?? string.Empty },
                    IsMandatory: true, SourceType: "Payment", SourceId: payment.Id),
                cancellationToken);
        }

        return Result.Success();
    }
}
