using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Abstractions;

namespace SmartTaxi.Application.Payments.Commands.FailPayment;

public sealed class FailPaymentCommandHandler : ICommandHandler<FailPaymentCommand, Result>
{
    private const string NotFailableError = "Ce paiement ne peut plus être marqué comme échoué.";

    private readonly IPaymentRepository _paymentRepository;

    public FailPaymentCommandHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<Result> Handle(FailPaymentCommand command, CancellationToken cancellationToken)
    {
        var failed = await _paymentRepository.TryFailAsync(command.PaymentId, command.Reason, DateTime.UtcNow, cancellationToken);

        return failed ? Result.Success() : Result.Failure(NotFailableError, ErrorType.Conflict);
    }
}
