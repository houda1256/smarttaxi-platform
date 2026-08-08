namespace SmartTaxi.Infrastructure.Rides.Options;

/// <summary>Development seed values, bound from configuration — never hardcoded permanent production prices.</summary>
public sealed class RidePricingOptions
{
    public const string SectionName = "RidePricing";

    public decimal BaseFare { get; init; } = 2m;
    public decimal PricePerKilometer { get; init; } = 0.8m;
    public decimal PricePerMinute { get; init; } = 0.1m;
    public decimal BookingFee { get; init; } = 1m;
    public decimal MinimumFare { get; init; } = 5m;
    public decimal WaitingFeePerMinute { get; init; } = 0.2m;
    public decimal CancellationFeeAfterAcceptance { get; init; } = 3m;
    public decimal CancellationFeeAfterArrival { get; init; } = 5m;

    /// <summary>Keyed by VehicleCategory enum name — unlisted categories default to no adjustment.</summary>
    public Dictionary<string, decimal> CategoryAdjustments { get; init; } = new()
    {
        ["Standard"] = 0m,
        ["Comfort"] = 3m,
        ["Van"] = 5m,
        ["Premium"] = 8m,
        ["Accessible"] = 0m
    };
}
