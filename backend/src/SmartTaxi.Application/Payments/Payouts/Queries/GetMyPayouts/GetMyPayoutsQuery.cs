using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetMyPayouts;

public sealed record GetMyPayoutsQuery(
    Guid BeneficiaryAccountId, PayoutStatus? Status, int PageNumber, int PageSize) : IQuery<PagedResult<Payout>>;
