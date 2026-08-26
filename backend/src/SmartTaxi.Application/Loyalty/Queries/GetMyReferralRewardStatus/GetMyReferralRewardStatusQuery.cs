using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyReferralRewardStatus;

public sealed record GetMyReferralRewardStatusQuery(Guid UserId) : IQuery<IReadOnlyCollection<LoyaltyReferralReward>>;
