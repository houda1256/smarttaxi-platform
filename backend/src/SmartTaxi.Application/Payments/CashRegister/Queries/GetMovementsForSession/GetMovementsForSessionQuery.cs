using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetMovementsForSession;

public sealed record GetMovementsForSessionQuery(Guid CashRegisterSessionId) : IQuery<IReadOnlyCollection<CashMovement>>;
