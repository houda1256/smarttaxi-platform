using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideShareTokenTests
{
    [Fact]
    public void Create_Succeeds_AndIsValidUntilExpiry()
    {
        var createdAt = DateTime.UtcNow;
        var token = RideShareToken.Create(Guid.NewGuid(), "hash-value", createdAt, TimeSpan.FromHours(2));

        Assert.True(token.IsValid(createdAt.AddHours(1)));
        Assert.False(token.IsValid(createdAt.AddHours(3)));
        Assert.Single(token.DomainEvents);
    }

    [Fact]
    public void Create_WithEmptyHash_Throws()
    {
        Assert.Throws<ArgumentException>(() => RideShareToken.Create(Guid.NewGuid(), "", DateTime.UtcNow, TimeSpan.FromHours(1)));
    }
}
