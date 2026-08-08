namespace SmartTaxi.Infrastructure.Payments.Options;

public sealed class PayoutOptions
{
    public const string SectionName = "Payout";

    public decimal MinimumPayoutAmount { get; init; } = 20m;
}
