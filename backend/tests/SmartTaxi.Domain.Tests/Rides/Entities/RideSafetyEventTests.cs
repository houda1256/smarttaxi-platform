using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;
using SmartTaxi.Domain.Rides.ValueObjects;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideSafetyEventTests
{
    [Fact]
    public void Trigger_Succeeds_WithOpenStatus()
    {
        var location = GeoCoordinate.Create(36.8065, 10.1815);
        var safetyEvent = RideSafetyEvent.Trigger(Guid.NewGuid(), Guid.NewGuid(), location, "Suspicious behavior", DateTime.UtcNow);

        Assert.Equal(RideSafetyEventStatus.Open, safetyEvent.Status);
        Assert.Single(safetyEvent.DomainEvents);
    }

    [Fact]
    public void Trigger_WithEmptyReason_Throws()
    {
        var location = GeoCoordinate.Create(36.8065, 10.1815);
        Assert.Throws<ArgumentException>(() => RideSafetyEvent.Trigger(Guid.NewGuid(), Guid.NewGuid(), location, "", DateTime.UtcNow));
    }
}
