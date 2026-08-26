using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyLoyaltySummary;

public sealed record GetMyLoyaltySummaryQuery(Guid UserId) : IQuery<LoyaltyAccountSummary?>;
