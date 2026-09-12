using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetTierThresholdsAdmin;

public sealed record GetTierThresholdsAdminQuery : IQuery<IReadOnlyCollection<LoyaltyTierThreshold>>;
