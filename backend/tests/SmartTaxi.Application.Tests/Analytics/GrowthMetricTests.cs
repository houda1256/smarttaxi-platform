using SmartTaxi.Application.Analytics.Contracts;

namespace SmartTaxi.Application.Tests.Analytics;

public class GrowthMetricTests
{
    [Fact]
    public void Compute_BothPeriodsZero_PercentageChangeIsZero()
    {
        var metric = GrowthMetric.Compute(0, 0);

        Assert.Equal(0m, metric.AbsoluteChange);
        Assert.Equal(0m, metric.PercentageChange);
    }

    [Fact]
    public void Compute_PreviousZeroCurrentPositive_PercentageChangeIsNull()
    {
        var metric = GrowthMetric.Compute(10, 0);

        Assert.Equal(10m, metric.AbsoluteChange);
        Assert.Null(metric.PercentageChange);
    }

    [Fact]
    public void Compute_NormalIncrease_ComputesExactPercentage()
    {
        var metric = GrowthMetric.Compute(150, 100);

        Assert.Equal(50m, metric.AbsoluteChange);
        Assert.Equal(50m, metric.PercentageChange);
    }

    [Fact]
    public void Compute_Decrease_ComputesNegativePercentage()
    {
        var metric = GrowthMetric.Compute(80, 100);

        Assert.Equal(-20m, metric.AbsoluteChange);
        Assert.Equal(-20m, metric.PercentageChange);
    }
}
