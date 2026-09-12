using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetRewardCatalog;

public sealed record GetRewardCatalogQuery(UserRole Role) : IQuery<IReadOnlyCollection<LoyaltyReward>>;
