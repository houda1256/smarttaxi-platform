using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Infrastructure.Rides.Services;

/// <summary>
/// A deterministic time-of-day band stub — not a demand/availability-driven
/// algorithm (no such signal exists yet). ZoneId is accepted for interface
/// stability but not yet used, since no zone catalog exists. Every multiplier
/// returned carries a human-readable explanation, per the "always explainable" rule.
/// </summary>
internal sealed class RideDynamicPricingProvider : IDynamicPricingProvider
{
    private const decimal PeakMultiplier = 1.3m;
    private const decimal NightMultiplier = 1.15m;
    private const decimal StandardMultiplier = 1.0m;

    public DynamicPricingResult GetMultiplier(DateTime utcNow, Guid? zoneId)
    {
        var hour = utcNow.Hour;
        var isPeak = hour is >= 7 and < 9 or >= 17 and < 19;
        var isNight = hour is >= 23 or < 5;

        if (isPeak)
        {
            return new DynamicPricingResult(PeakMultiplier, "Heure de pointe (7h-9h ou 17h-19h)");
        }

        if (isNight)
        {
            return new DynamicPricingResult(NightMultiplier, "Tarif de nuit (23h-5h)");
        }

        return new DynamicPricingResult(StandardMultiplier, "Tarif standard");
    }
}
