using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.CashRegister.Entities;
using SmartTaxi.Domain.Payments.CashRegister.Enums;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetSessionsForOwner;

public sealed record GetSessionsForOwnerQuery(
    Guid OwnerId, CashRegisterSessionStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<CashRegisterSession>>;
