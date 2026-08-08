using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Commands.OpenCashRegisterSession;

/// <summary>Rejects a second concurrent Open session on the same register via TryAddAsync's own DB-level guard, rather than a non-atomic "check then insert" race.</summary>
public sealed class OpenCashRegisterSessionCommandHandler : ICommandHandler<OpenCashRegisterSessionCommand, Result<Guid>>
{
    private const string NotFoundError = "Caisse introuvable.";
    private const string AlreadyOpenError = "Une session est déjà ouverte pour cette caisse.";

    private readonly ICashRegisterRepository _cashRegisterRepository;
    private readonly ICashRegisterSessionRepository _sessionRepository;

    public OpenCashRegisterSessionCommandHandler(
        ICashRegisterRepository cashRegisterRepository, ICashRegisterSessionRepository sessionRepository)
    {
        _cashRegisterRepository = cashRegisterRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<Guid>> Handle(OpenCashRegisterSessionCommand command, CancellationToken cancellationToken)
    {
        var cashRegister = await _cashRegisterRepository.GetByIdAsync(command.CashRegisterId, cancellationToken);

        if (cashRegister is null)
        {
            return Result<Guid>.Failure(NotFoundError, ErrorType.NotFound);
        }

        CashRegisterSession session;

        try
        {
            session = CashRegisterSession.Open(command.CashRegisterId, command.OpenedBy, command.OpeningBalance, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _sessionRepository.TryAddAsync(session, cancellationToken);

        return added ? Result<Guid>.Success(session.Id) : Result<Guid>.Failure(AlreadyOpenError, ErrorType.Conflict);
    }
}
