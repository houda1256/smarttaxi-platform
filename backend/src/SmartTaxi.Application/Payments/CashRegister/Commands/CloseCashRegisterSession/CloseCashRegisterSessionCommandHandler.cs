using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.CloseCashRegisterSession;

/// <summary>
/// ClosingExpectedBalance is always computed here from the session's own
/// CashMovement rows — never trusted from the caller — so it cannot be
/// tampered with. Any non-zero difference between expected and the actor's
/// declared actual balance requires a DifferenceReason (the master prompt's
/// "cash differences require justification" rule); returns the resulting
/// Difference so the caller can display it immediately.
/// </summary>
public sealed class CloseCashRegisterSessionCommandHandler : ICommandHandler<CloseCashRegisterSessionCommand, Result<decimal>>
{
    private const string NotFoundError = "Session de caisse introuvable.";
    private const string NotOpenError = "Cette session de caisse n'est pas ouverte.";
    private const string ReasonRequiredError = "Un motif est requis lorsque le solde réel diffère du solde attendu.";

    private readonly ICashRegisterSessionRepository _sessionRepository;
    private readonly ICashMovementRepository _movementRepository;

    public CloseCashRegisterSessionCommandHandler(ICashRegisterSessionRepository sessionRepository, ICashMovementRepository movementRepository)
    {
        _sessionRepository = sessionRepository;
        _movementRepository = movementRepository;
    }

    public async Task<Result<decimal>> Handle(CloseCashRegisterSessionCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(command.CashRegisterSessionId, cancellationToken);

        if (session is null)
        {
            return Result<decimal>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (session.Status != CashRegisterSessionStatus.Open)
        {
            return Result<decimal>.Failure(NotOpenError, ErrorType.Conflict);
        }

        var movementsNetTotal = await _movementRepository.GetNetTotalForSessionAsync(command.CashRegisterSessionId, cancellationToken);
        var expectedBalance = session.OpeningBalance + movementsNetTotal;
        var difference = command.ClosingActualBalance - expectedBalance;

        if (difference != 0 && string.IsNullOrWhiteSpace(command.DifferenceReason))
        {
            return Result<decimal>.Failure(ReasonRequiredError, ErrorType.Validation);
        }

        var closed = await _sessionRepository.TryCloseAsync(
            command.CashRegisterSessionId, expectedBalance, command.ClosingActualBalance, difference,
            command.DifferenceReason, DateTime.UtcNow, cancellationToken);

        return closed ? Result<decimal>.Success(difference) : Result<decimal>.Failure(NotOpenError, ErrorType.Conflict);
    }
}
