using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Loyalty.Commands.RedeemReward;

/// <summary>
/// IdempotencyKey is client-supplied — a mobile client generates it once per
/// redemption intent and resends the exact same value on any retry (timeout,
/// double-tap), so the same intent always maps to the same redemption instead
/// of debiting the user's points twice (Module 7 audit fix #3).
/// </summary>
public sealed record RedeemRewardCommand(Guid UserId, Guid RewardId, string IdempotencyKey) : ICommand<Result<Guid>>;
