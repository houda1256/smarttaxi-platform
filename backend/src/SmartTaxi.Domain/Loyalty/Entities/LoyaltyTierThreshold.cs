using SmartTaxi.Domain.Common;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty.Entities;

/// <summary>Admin-managed catalog, one row per Tier — deliberately separate from LoyaltyAccount so thresholds can be retuned without ever touching account/ledger rows.</summary>
public sealed class LoyaltyTierThreshold : AggregateRoot
{
    public LoyaltyTier Tier { get; private set; }
    public int MinimumStatusPoints { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LoyaltyTierThreshold()
    {
    }

    private LoyaltyTierThreshold(LoyaltyTier tier, int minimumStatusPoints, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        Tier = tier;
        MinimumStatusPoints = minimumStatusPoints;
        CreatedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public static LoyaltyTierThreshold Create(LoyaltyTier tier, int minimumStatusPoints, DateTime utcNow)
    {
        if (minimumStatusPoints < 0)
        {
            throw new ArgumentException("Le seuil de points de statut ne peut pas être négatif.");
        }

        return new LoyaltyTierThreshold(tier, minimumStatusPoints, utcNow);
    }

    public void UpdateThreshold(int minimumStatusPoints, DateTime utcNow)
    {
        if (minimumStatusPoints < 0)
        {
            throw new ArgumentException("Le seuil de points de statut ne peut pas être négatif.");
        }

        MinimumStatusPoints = minimumStatusPoints;
        UpdatedAtUtc = utcNow;
    }
}
