using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty.Entities;

public sealed class LoyaltyReward : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int CostInRewardPoints { get; private set; }
    public LoyaltyRewardType RewardType { get; private set; }
    public IReadOnlyList<UserRole> TargetRoles { get; private set; } = [];
    public bool IsActive { get; private set; }
    public DateTime? AvailableFrom { get; private set; }
    public DateTime? AvailableTo { get; private set; }
    public int? UsageLimit { get; private set; }
    public int RedeemedCount { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    /// <summary>The audit proved Payments has no discount/voucher/price-adjustment hook — RideDiscount/SubscriptionDiscount are modeled for catalog completeness but cannot be fulfilled today.</summary>
    public bool IsExecutable => RewardType is LoyaltyRewardType.FreeService or LoyaltyRewardType.PartnerOffer or LoyaltyRewardType.PromotionalCoupon;

    private LoyaltyReward()
    {
    }

    private LoyaltyReward(
        string code, string name, string description, int costInRewardPoints, LoyaltyRewardType rewardType,
        IReadOnlyList<UserRole> targetRoles, DateTime? availableFrom, DateTime? availableTo, int? usageLimit, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Code = code;
        Name = name;
        Description = description;
        CostInRewardPoints = costInRewardPoints;
        RewardType = rewardType;
        TargetRoles = targetRoles;
        IsActive = true;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        UsageLimit = usageLimit;
        RedeemedCount = 0;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static LoyaltyReward Create(
        string code, string name, string description, int costInRewardPoints, LoyaltyRewardType rewardType,
        IReadOnlyList<UserRole> targetRoles, DateTime? availableFrom, DateTime? availableTo, int? usageLimit, DateTime utcNow)
    {
        Validate(code, name, costInRewardPoints, usageLimit, availableFrom, availableTo);

        return new LoyaltyReward(
            code.Trim().ToUpperInvariant(), name.Trim(), description?.Trim() ?? string.Empty, costInRewardPoints, rewardType,
            targetRoles, availableFrom, availableTo, usageLimit, utcNow);
    }

    public void UpdateDetails(
        string name, string description, int costInRewardPoints, DateTime? availableFrom, DateTime? availableTo, int? usageLimit,
        DateTime utcNow)
    {
        Validate(Code, name, costInRewardPoints, usageLimit, availableFrom, availableTo);

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        CostInRewardPoints = costInRewardPoints;
        AvailableFrom = availableFrom;
        AvailableTo = availableTo;
        UsageLimit = usageLimit;
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

    public bool IsAvailable(DateTime utcNow) =>
        IsActive
        && (AvailableFrom is null || AvailableFrom <= utcNow)
        && (AvailableTo is null || AvailableTo >= utcNow)
        && (UsageLimit is null || RedeemedCount < UsageLimit.Value);

    public bool IsAvailableForRole(UserRole role) => TargetRoles.Count == 0 || TargetRoles.Contains(role);

    /// <summary>In-memory projection only — the real, race-safe increment is an atomic conditional UPDATE at the repository layer guarded on UsageLimit, same convention as LoyaltyAccount's cached balances.</summary>
    public void IncrementRedeemedCount(DateTime utcNow)
    {
        RedeemedCount++;
        UpdatedAtUtc = utcNow;
    }

    private static void Validate(string code, string name, int costInRewardPoints, int? usageLimit, DateTime? availableFrom, DateTime? availableTo)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Le code de la récompense est requis.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Le nom de la récompense est requis.");
        }

        if (costInRewardPoints <= 0)
        {
            throw new ArgumentException("Le coût en points doit être positif.");
        }

        if (usageLimit is not null && usageLimit <= 0)
        {
            throw new ArgumentException("La limite d'utilisation doit être positive.");
        }

        if (availableFrom is not null && availableTo is not null && availableTo < availableFrom)
        {
            throw new ArgumentException("La date de fin de disponibilité ne peut pas précéder la date de début.");
        }
    }
}
