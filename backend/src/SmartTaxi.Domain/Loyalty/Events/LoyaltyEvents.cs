using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty.Events;

/// <summary>Genuinely raised (unit-testable) — same convention as every other module's Created event. No dispatcher/outbox consumes these; INotificationDispatcher is the real integration point Loyalty's own Application handlers call directly.</summary>
public sealed record LoyaltyAccountCreated(Guid LoyaltyAccountId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record LoyaltyTierChanged(Guid LoyaltyAccountId, Guid UserId, LoyaltyTier PreviousTier, LoyaltyTier NewTier, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record RewardRedeemed(Guid RedemptionId, Guid UserId, Guid RewardId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record ReferralRewardGranted(Guid LoyaltyReferralRewardId, Guid ReferralId, Guid ReferrerUserId, Guid RefereeUserId, DateTime OccurredAtUtc) : IDomainEvent;

public sealed record ChallengeCompleted(Guid ProgressId, Guid ChallengeId, Guid UserId, DateTime OccurredAtUtc) : IDomainEvent;
