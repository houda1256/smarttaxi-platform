using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class SharedRideMatchTests
{
    [Fact]
    public void Suggest_Succeeds_WithMatchingStatus()
    {
        var match = SharedRideMatch.Suggest(DateTime.UtcNow, TimeSpan.FromMinutes(5));

        Assert.Equal(SharedRideMatchStatus.Matching, match.Status);
        Assert.Single(match.DomainEvents);
    }

    [Fact]
    public void Suggest_WithZeroExpiry_Throws()
    {
        Assert.Throws<ArgumentException>(() => SharedRideMatch.Suggest(DateTime.UtcNow, TimeSpan.Zero));
    }

    [Fact]
    public void IsExpired_AfterExpiresAt_ReturnsTrue()
    {
        var createdAt = DateTime.UtcNow;
        var match = SharedRideMatch.Suggest(createdAt, TimeSpan.FromMinutes(5));

        Assert.False(match.IsExpired(createdAt.AddMinutes(2)));
        Assert.True(match.IsExpired(createdAt.AddMinutes(6)));
    }
}
