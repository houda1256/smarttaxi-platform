namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record CreateChallengeRequest(
    string Code, string Name, string CriteriaType, int TargetValue, int RewardPoints, string? EligibleRole, DateTime? ValidFrom, DateTime? ValidTo);
