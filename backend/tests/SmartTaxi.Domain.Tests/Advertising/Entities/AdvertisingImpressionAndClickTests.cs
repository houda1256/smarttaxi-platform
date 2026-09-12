using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Domain.Tests.Advertising.Entities;

public class AdvertisingImpressionAndClickTests
{
    [Fact]
    public void Impression_Record_WithValidFields_Succeeds()
    {
        var impression = AdvertisingImpression.Record(Guid.NewGuid(), Guid.NewGuid(), "idem-1", 0.05m, DateTime.UtcNow, DateTime.UtcNow);

        Assert.Equal("idem-1", impression.IdempotencyKey);
        Assert.Equal(0.05m, impression.OperationalCost);
    }

    [Fact]
    public void Impression_Record_BlankIdempotencyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => AdvertisingImpression.Record(Guid.NewGuid(), Guid.NewGuid(), " ", 0m, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Impression_Record_NegativeCost_Throws()
    {
        Assert.Throws<ArgumentException>(() => AdvertisingImpression.Record(Guid.NewGuid(), Guid.NewGuid(), "idem-1", -1m, DateTime.UtcNow, DateTime.UtcNow));
    }

    [Fact]
    public void Click_Record_WithoutImpressionId_Succeeds()
    {
        var click = AdvertisingClick.Record(Guid.NewGuid(), Guid.NewGuid(), null, "idem-click-1", 0.10m, DateTime.UtcNow, DateTime.UtcNow);

        Assert.Null(click.ImpressionId);
        Assert.Equal(0.10m, click.OperationalCost);
    }

    [Fact]
    public void Click_Record_BlankIdempotencyKey_Throws()
    {
        Assert.Throws<ArgumentException>(() => AdvertisingClick.Record(Guid.NewGuid(), Guid.NewGuid(), null, "", 0m, DateTime.UtcNow, DateTime.UtcNow));
    }
}
