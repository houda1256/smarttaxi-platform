namespace SmartTaxi.Application.Analytics.Contracts;

/// <summary>
/// PercentageChange is null when PreviousPeriod is 0 and CurrentPeriod is
/// greater than 0 — mathematically undefined growth, never fabricated as
/// "100%" or "∞". Both zero yields 0, not null. Amounts are decimal
/// uniformly (counts included) so the same shape serves both count-based and
/// currency-based metrics.
/// </summary>
public sealed record GrowthMetric(decimal CurrentPeriod, decimal PreviousPeriod, decimal AbsoluteChange, decimal? PercentageChange)
{
    public static GrowthMetric Compute(decimal currentPeriod, decimal previousPeriod)
    {
        var absoluteChange = currentPeriod - previousPeriod;

        decimal? percentageChange = previousPeriod switch
        {
            0 when currentPeriod == 0 => 0m,
            0 => null,
            _ => Math.Round(absoluteChange / previousPeriod * 100m, 2)
        };

        return new GrowthMetric(currentPeriod, previousPeriod, absoluteChange, percentageChange);
    }
}
