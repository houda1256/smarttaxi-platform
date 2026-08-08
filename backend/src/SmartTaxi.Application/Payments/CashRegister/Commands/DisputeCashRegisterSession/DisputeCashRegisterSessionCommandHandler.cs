using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.DisputeCashRegisterSession;

public sealed class DisputeCashRegisterSessionCommandHandler : ICommandHandler<DisputeCashRegisterSessionCommand, Result>
{
    private const string NotFoundError = "Session de caisse introuvable.";
    private const string NotClosedError = "Seule une session clôturée peut être mise en litige.";
    private const string ReasonRequiredError = "Un motif est requis pour mettre une session en litige.";

    private readonly ICashRegisterSessionRepository _sessionRepository;

    public DisputeCashRegisterSessionCommandHandler(ICashRegisterSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result> Handle(DisputeCashRegisterSessionCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var session = await _sessionRepository.GetByIdAsync(command.CashRegisterSessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var disputed = await _sessionRepository.TryDisputeAsync(command.CashRegisterSessionId, command.Reason, DateTime.UtcNow, cancellationToken);

        return disputed ? Result.Success() : Result.Failure(NotClosedError, ErrorType.Conflict);
    }
}
