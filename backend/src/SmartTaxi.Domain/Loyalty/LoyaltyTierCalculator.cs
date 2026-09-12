using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Domain.Loyalty.Enums;

namespace SmartTaxi.Domain.Loyalty;

/// <summary>
/// Pure, deterministic tier derivation from StatusPoints — the same catalog
/// (re-)evaluated at any time always yields the same Tier for the same
/// StatusPoints total. Changing thresholds later never rewrites ledger
/// history; it only changes what a given StatusPoints total maps to going
/// forward. Falls back to Bronze if no threshold catalog is configured.
/// </summary>
public static class LoyaltyTierCalculator
{
    public static LoyaltyTier Determine(int statusPoints, IReadOnlyCollection<LoyaltyTierThreshold> thresholds)
    {
        var highestReached = thresholds
            .Where(threshold => statusPoints >= threshold.MinimumStatusPoints)
            .OrderByDescending(threshold => threshold.MinimumStatusPoints)
            .FirstOrDefault();

        return highestReached?.Tier ?? LoyaltyTier.Bronze;
    }
}
