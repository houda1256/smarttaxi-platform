namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record CreateRewardRequest(
    string Code, string Name, string Description, int CostInRewardPoints, string RewardType, IReadOnlyList<string> TargetRoles,
    DateTime? AvailableFrom, DateTime? AvailableTo, int? UsageLimit);

public sealed record UpdateRewardRequest(string Name, string Description, int CostInRewardPoints, DateTime? AvailableFrom, DateTime? AvailableTo, int? UsageLimit);

/// <summary>IdempotencyKey should be a fresh client-generated value (e.g. a UUID) per redemption intent, resent unchanged on any retry of that same intent.</summary>
public sealed record RedeemRewardRequest(string IdempotencyKey);
