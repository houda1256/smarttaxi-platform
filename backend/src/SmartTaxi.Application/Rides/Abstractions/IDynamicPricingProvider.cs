namespace SmartTaxi.Application.Rides.Abstractions;

/// <summary>A deterministic stub (e.g. time-of-day bands) — not a demand-driven algorithm. Every applied multiplier is returned with a human-readable explanation.</summary>
public interface IDynamicPricingProvider
{
    DynamicPricingResult GetMultiplier(DateTime utcNow, Guid? zoneId);
}

public sealed record DynamicPricingResult(decimal Multiplier, string Explanation);
