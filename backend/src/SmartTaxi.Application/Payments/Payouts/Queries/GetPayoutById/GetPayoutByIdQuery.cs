using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetPayoutById;

public sealed record GetPayoutByIdQuery(Guid PayoutId) : IQuery<Result<Payout>>;
