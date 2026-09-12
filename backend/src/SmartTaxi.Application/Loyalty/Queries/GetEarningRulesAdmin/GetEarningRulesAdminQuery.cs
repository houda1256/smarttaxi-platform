using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetEarningRulesAdmin;

public sealed record GetEarningRulesAdminQuery : IQuery<IReadOnlyCollection<LoyaltyEarningRule>>;
