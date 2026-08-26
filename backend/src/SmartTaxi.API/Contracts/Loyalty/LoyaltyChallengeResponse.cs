using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.API.Contracts.Loyalty;

public sealed record LoyaltyChallengeResponse(
    Guid Id, string Code, string Name, string CriteriaType, int TargetValue, int RewardPoints, string? EligibleRole, bool IsActive,
    DateTime? ValidFrom, DateTime? ValidTo)
{
    public static LoyaltyChallengeResponse FromEntity(LoyaltyChallenge challenge) => new(
        challenge.Id, challenge.Code, challenge.Name, challenge.CriteriaType.ToString(), challenge.TargetValue, challenge.RewardPoints,
        challenge.EligibleRole?.ToString(), challenge.IsActive, challenge.ValidFrom, challenge.ValidTo);
}
