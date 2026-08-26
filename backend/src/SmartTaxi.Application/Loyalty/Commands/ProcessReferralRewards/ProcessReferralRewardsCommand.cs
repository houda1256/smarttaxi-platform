using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.ProcessReferralRewards;

/// <summary>Bulk catch-up sweep over every Identity referral Identity has already marked RewardEligible — a safety net beyond the real-time grant already attempted inside ProcessPaymentLoyaltyAwardCommand. Idempotent: LoyaltyReferralReward's unique ReferralId index means re-running this never double-rewards.</summary>
public sealed record ProcessReferralRewardsCommand : ICommand<Result<int>>;
