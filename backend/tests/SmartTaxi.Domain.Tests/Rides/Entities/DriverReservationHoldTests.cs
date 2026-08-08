using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class DriverReservationHoldTests
{
    [Fact]
    public void CreateActive_Succeeds_WithActiveStatusAndComputedExpiry()
    {
        var heldAt = DateTime.UtcNow;
        var hold = DriverReservationHold.CreateActive(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), heldAt, TimeSpan.FromSeconds(20));

        Assert.Equal(DriverReservationHoldStatus.Active, hold.Status);
        Assert.Equal(heldAt.AddSeconds(20), hold.ExpiresAt);
        Assert.Single(hold.DomainEvents);
    }

    [Fact]
    public void CreateActive_WithZeroDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            DriverReservationHold.CreateActive(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, TimeSpan.Zero));
    }

    [Fact]
    public void IsExpired_AfterExpiresAt_ReturnsTrue()
    {
        var heldAt = DateTime.UtcNow;
        var hold = DriverReservationHold.CreateActive(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), heldAt, TimeSpan.FromSeconds(20));

        Assert.False(hold.IsExpired(heldAt.AddSeconds(10)));
        Assert.True(hold.IsExpired(heldAt.AddSeconds(21)));
    }
}
