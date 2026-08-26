using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetRewardCatalogAdmin;

public sealed record GetRewardCatalogAdminQuery : IQuery<IReadOnlyCollection<LoyaltyReward>>;
