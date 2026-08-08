using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.ReconcileCashRegisterSession;

public sealed class ReconcileCashRegisterSessionCommandHandler : ICommandHandler<ReconcileCashRegisterSessionCommand, Result>
{
    private const string NotFoundError = "Session de caisse introuvable.";
    private const string NotClosedError = "Seule une session clôturée peut être réconciliée.";

    private readonly ICashRegisterSessionRepository _sessionRepository;

    public ReconcileCashRegisterSessionCommandHandler(ICashRegisterSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result> Handle(ReconcileCashRegisterSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(command.CashRegisterSessionId, cancellationToken);

        if (session is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var reconciled = await _sessionRepository.TryReconcileAsync(command.CashRegisterSessionId, DateTime.UtcNow, cancellationToken);

        return reconciled ? Result.Success() : Result.Failure(NotClosedError, ErrorType.Conflict);
    }
}
