namespace SmartTaxi.Domain.Advertising.Enums;

/// <summary>Flat/Duration are a single total price for the whole campaign; Cpm/Cpc are per-1000-impressions/per-click rates applied as impressions/clicks are recorded.</summary>
public enum AdPricingModel
{
    Flat,
    Duration,
    Cpm,
    Cpc
}
