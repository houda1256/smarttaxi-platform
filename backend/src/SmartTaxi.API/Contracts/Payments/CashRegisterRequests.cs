using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.API.Contracts.Payments;

public sealed record RegisterCashRegisterRequest(string Label);

public sealed record OpenCashRegisterSessionRequest(Guid CashRegisterId, decimal OpeningBalance);

public sealed record RecordCashMovementRequest(CashMovementType MovementType, decimal Amount, string? Description);

public sealed record CloseCashRegisterSessionRequest(decimal ClosingActualBalance, string? DifferenceReason);

public sealed record DisputeCashRegisterSessionRequest(string Reason);
