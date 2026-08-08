namespace SmartTaxi.Infrastructure.Payments.Options;

public sealed class PlatformCommissionOptions
{
    public const string SectionName = "PlatformCommission";

    public decimal CommissionPercentage { get; init; } = 10m;
}
