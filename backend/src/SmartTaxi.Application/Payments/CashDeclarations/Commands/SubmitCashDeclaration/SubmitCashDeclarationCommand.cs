using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Payments.CashDeclarations.Commands.SubmitCashDeclaration;

public sealed record SubmitCashDeclarationCommand(
    Guid DriverId, Guid OwnerId, Guid? AssignmentId, DateOnly PeriodStart, DateOnly PeriodEnd,
    decimal ExpectedCash, decimal DeclaredCash) : ICommand<Result<Guid>>;
