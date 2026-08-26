using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyPointLedger;

public sealed record GetMyPointLedgerQuery(Guid UserId, int PageNumber, int PageSize) : IQuery<PagedResult<LoyaltyPointLedgerEntry>>;
