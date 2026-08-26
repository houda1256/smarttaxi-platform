using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>Admin-managed catalog. Deliberately small (no badges, no giant gamification framework) — CriteriaType is limited to what Loyalty can evaluate from its own ledger data (see LoyaltyChallengeCriteriaType).</summary>
public sealed class LoyaltyChallenge : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public LoyaltyChallengeCriteriaType CriteriaType { get; private set; }
    public int TargetValue { get; private set; }
    public int RewardPoints { get; private set; }
    public UserRole? EligibleRole { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LoyaltyChallenge()
    {
    }

    private LoyaltyChallenge(
        string code, string name, LoyaltyChallengeCriteriaType criteriaType, int targetValue, int rewardPoints,
        UserRole? eligibleRole, DateTime? validFrom, DateTime? validTo, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        CriteriaType = criteriaType;
        TargetValue = targetValue;
        RewardPoints = rewardPoints;
        EligibleRole = eligibleRole;
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = true;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static LoyaltyChallenge Create(
        string code, string name, LoyaltyChallengeCriteriaType criteriaType, int targetValue, int rewardPoints,
        UserRole? eligibleRole, DateTime? validFrom, DateTime? validTo, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code du défi est requis.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom du défi est requis.");
        }

        if (targetValue <= 0)
        {
            throw new ArgumentException("La valeur cible doit être positive.");
        }

        if (rewardPoints <= 0)
        {
            throw new ArgumentException("La récompense en points doit être positive.");
        }

        if (validFrom is not null && validTo is not null && validTo < validFrom)
        {
            throw new ArgumentException("La date de fin de validité ne peut pas précéder la date de début.");
        }

        return new LoyaltyChallenge(code.Trim().ToUpperInvariant(), name.Trim(), criteriaType, targetValue, rewardPoints, eligibleRole, validFrom, validTo, utcNow);
    }

    public void Activate(DateTime utcNow)
    {
        IsActive = true;
        UpdatedAtUtc = utcNow;
    }

    public void Deactivate(DateTime utcNow)
    {
        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public bool IsEffectiveFor(UserRole role, DateTime utcNow) =>
        IsActive
        && (EligibleRole is null || EligibleRole == role)
        && (ValidFrom is null || ValidFrom <= utcNow)
        && (ValidTo is null || ValidTo >= utcNow);
}
