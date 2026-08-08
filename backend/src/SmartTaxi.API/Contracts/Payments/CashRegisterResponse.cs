using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record CashRegisterResponse(Guid Id, Guid OwnerId, string Label, DateTime CreatedAt)
{
    public static CashRegisterResponse FromEntity(CashRegisterBox cashRegister) =>
        new(cashRegister.Id, cashRegister.OwnerId, cashRegister.Label, cashRegister.CreatedAt);
}

public sealed record CashRegisterSessionResponse(
    Guid Id,
    Guid CashRegisterId,
    Guid OpenedBy,
    DateTime OpenedAt,
    decimal OpeningBalance,
    decimal? ClosingExpectedBalance,
    decimal? ClosingActualBalance,
    decimal? Difference,
    string? DifferenceReason,
    DateTime? ClosedAt,
    string Status)
{
    public static CashRegisterSessionResponse FromEntity(CashRegisterSession session) => new(
        session.Id, session.CashRegisterId, session.OpenedBy, session.OpenedAt, session.OpeningBalance,
        session.ClosingExpectedBalance, session.ClosingActualBalance, session.Difference, session.DifferenceReason,
        session.ClosedAt, session.Status.ToString());
}

public sealed record CashMovementResponse(
    Guid Id, Guid CashRegisterSessionId, string MovementType, decimal Amount, string? Description, DateTime RecordedAt, Guid RecordedBy)
{
    public static CashMovementResponse FromEntity(CashMovement movement) => new(
        movement.Id, movement.CashRegisterSessionId, movement.MovementType.ToString(), movement.Amount, movement.Description,
        movement.RecordedAt, movement.RecordedBy);
}
