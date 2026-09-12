using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetCashRegisterSessionById;

public sealed record GetCashRegisterSessionByIdQuery(Guid CashRegisterSessionId) : IQuery<Result<CashRegisterSession>>;
