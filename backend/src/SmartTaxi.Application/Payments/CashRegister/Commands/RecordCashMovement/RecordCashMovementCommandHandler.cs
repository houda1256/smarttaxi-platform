using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.RecordCashMovement;

public sealed class RecordCashMovementCommandHandler : ICommandHandler<RecordCashMovementCommand, Result<Guid>>
{
    private const string SessionNotFoundError = "Session de caisse introuvable.";
    private const string SessionNotOpenError = "Impossible d'enregistrer un mouvement sur une session de caisse qui n'est pas ouverte.";

    private readonly ICashRegisterSessionRepository _sessionRepository;
    private readonly ICashMovementRepository _movementRepository;

    public RecordCashMovementCommandHandler(ICashRegisterSessionRepository sessionRepository, ICashMovementRepository movementRepository)
    {
        _sessionRepository = sessionRepository;
        _movementRepository = movementRepository;
    }

    public async Task<Result<Guid>> Handle(RecordCashMovementCommand command, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(command.CashRegisterSessionId, cancellationToken);

        if (session is null)
        {
            return Result<Guid>.Failure(SessionNotFoundError, ErrorType.NotFound);
        }

        if (session.Status != CashRegisterSessionStatus.Open)
        {
            return Result<Guid>.Failure(SessionNotOpenError, ErrorType.Conflict);
        }

        CashMovement movement;

        try
        {
            movement = new CashMovement(command.CashRegisterSessionId, command.MovementType, command.Amount, command.Description, command.RecordedBy, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _movementRepository.AddAsync(movement, cancellationToken);

        return Result<Guid>.Success(movement.Id);
    }
}
