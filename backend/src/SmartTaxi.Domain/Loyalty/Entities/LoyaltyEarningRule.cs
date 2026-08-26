using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>
/// Admin-managed catalog — commercial numbers (points-per-currency-unit,
/// bounds) live here, never hard-coded in a command handler. Uses decimal
/// throughout (never float/double) for the same reason Payments/Subscriptions
/// do; CalculatePoints floors to whole points (points are discrete) — see its
/// own doc comment for the exact, tested rounding rule.
/// </summary>
public sealed class LoyaltyEarningRule : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public UserRole ActorRole { get; private set; }
    public string SourceType { get; private set; } = string.Empty;
    public decimal RewardPointsPerCurrencyUnit { get; private set; }
    public decimal StatusPointsPerCurrencyUnit { get; private set; }
    public int? MinPoints { get; private set; }
    public int? MaxPoints { get; private set; }
    public bool SubscriptionMultiplierAllowed { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime? ValidFrom { get; private set; }
    public DateTime? ValidTo { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LoyaltyEarningRule()
    {
    }

    private LoyaltyEarningRule(
        string code, UserRole actorRole, string sourceType, decimal rewardPointsPerCurrencyUnit, decimal statusPointsPerCurrencyUnit,
        int? minPoints, int? maxPoints, bool subscriptionMultiplierAllowed, DateTime? validFrom, DateTime? validTo, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Code = code;
        ActorRole = actorRole;
        SourceType = sourceType;
        RewardPointsPerCurrencyUnit = rewardPointsPerCurrencyUnit;
        StatusPointsPerCurrencyUnit = statusPointsPerCurrencyUnit;
        MinPoints = minPoints;
        MaxPoints = maxPoints;
        SubscriptionMultiplierAllowed = subscriptionMultiplierAllowed;
        IsActive = true;
        ValidFrom = validFrom;
        ValidTo = validTo;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static LoyaltyEarningRule Create(
        string code, UserRole actorRole, string sourceType, decimal rewardPointsPerCurrencyUnit, decimal statusPointsPerCurrencyUnit,
        int? minPoints, int? maxPoints, bool subscriptionMultiplierAllowed, DateTime? validFrom, DateTime? validTo, DateTime utcNow)
    {
        Validate(code, sourceType, rewardPointsPerCurrencyUnit, statusPointsPerCurrencyUnit, minPoints, maxPoints, validFrom, validTo);

        return new LoyaltyEarningRule(
            code.Trim().ToUpperInvariant(), actorRole, sourceType.Trim(), rewardPointsPerCurrencyUnit, statusPointsPerCurrencyUnit,
            minPoints, maxPoints, subscriptionMultiplierAllowed, validFrom, validTo, utcNow);
    }

    public void UpdateDetails(
        decimal rewardPointsPerCurrencyUnit, decimal statusPointsPerCurrencyUnit, int? minPoints, int? maxPoints,
        bool subscriptionMultiplierAllowed, DateTime? validFrom, DateTime? validTo, DateTime utcNow)
    {
        Validate(Code, SourceType, rewardPointsPerCurrencyUnit, statusPointsPerCurrencyUnit, minPoints, maxPoints, validFrom, validTo);

        RewardPointsPerCurrencyUnit = rewardPointsPerCurrencyUnit;
        StatusPointsPerCurrencyUnit = statusPointsPerCurrencyUnit;
        MinPoints = minPoints;
        MaxPoints = maxPoints;
        SubscriptionMultiplierAllowed = subscriptionMultiplierAllowed;
        ValidFrom = validFrom;
        ValidTo = validTo;
        UpdatedAtUtc = utcNow;
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

    public bool IsEffective(DateTime utcNow) =>
        IsActive && (ValidFrom is null || ValidFrom <= utcNow) && (ValidTo is null || ValidTo >= utcNow);

    /// <summary>
    /// Floors (never rounds up) both point types to whole points after applying
    /// the multiplier — a conservative, deterministic rule so the platform never
    /// grants more than the rule's rate strictly allows. MinPoints/MaxPoints
    /// bound RewardPoints only (StatusPoints has no configured bounds today).
    /// </summary>
    public (int RewardPoints, int StatusPoints) CalculatePoints(decimal amount, decimal subscriptionMultiplierPercent)
    {
        var effectiveMultiplierPercent = SubscriptionMultiplierAllowed ? subscriptionMultiplierPercent : 100m;

        var rawReward = amount * RewardPointsPerCurrencyUnit * effectiveMultiplierPercent / 100m;
        var rawStatus = amount * StatusPointsPerCurrencyUnit * effectiveMultiplierPercent / 100m;

        var rewardPoints = Math.Max(0, (int)Math.Floor(rawReward));
        var statusPoints = Math.Max(0, (int)Math.Floor(rawStatus));

        if (MinPoints is not null)
        {
            rewardPoints = Math.Max(rewardPoints, MinPoints.Value);
        }

        if (MaxPoints is not null)
        {
            rewardPoints = Math.Min(rewardPoints, MaxPoints.Value);
        }

        return (rewardPoints, statusPoints);
    }

    private static void Validate(
        string code, string sourceType, decimal rewardRate, decimal statusRate, int? minPoints, int? maxPoints,
        DateTime? validFrom, DateTime? validTo)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de la règle est requis.");
        }

        if (string.IsNullOrWhiteSpace(sourceType))
        {
            throw new ArgumentException("Le type de source est requis.");
        }

        if (rewardRate < 0 || statusRate < 0)
        {
            throw new ArgumentException("Les taux de points ne peuvent pas être négatifs.");
        }

        if (minPoints is not null && minPoints < 0)
        {
            throw new ArgumentException("Le minimum de points ne peut pas être négatif.");
        }

        if (maxPoints is not null && minPoints is not null && maxPoints < minPoints)
        {
            throw new ArgumentException("Le maximum de points ne peut pas être inférieur au minimum.");
        }

        if (validFrom is not null && validTo is not null && validTo < validFrom)
        {
            throw new ArgumentException("La date de fin de validité ne peut pas précéder la date de début.");
        }
    }
}
