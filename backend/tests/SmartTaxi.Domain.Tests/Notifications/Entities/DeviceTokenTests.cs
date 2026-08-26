using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Domain.Tests.Notifications.Entities;

public class DeviceTokenTests
{
    [Fact]
    public void Register_WithValidData_IsActive()
    {
        var token = DeviceToken.Register(Guid.NewGuid(), "raw-token", "android", DateTime.UtcNow);

        Assert.True(token.IsActive);
        Assert.Null(token.RevokedAtUtc);
    }

    [Fact]
    public void Register_WithEmptyToken_Throws()
    {
        Assert.Throws<ArgumentException>(() => DeviceToken.Register(Guid.NewGuid(), "", "android", DateTime.UtcNow));
    }

    [Fact]
    public void Revoke_SetsIsActiveFalse()
    {
        var token = DeviceToken.Register(Guid.NewGuid(), "raw-token", "ios", DateTime.UtcNow);

        token.Revoke(DateTime.UtcNow);

        Assert.False(token.IsActive);
    }

    [Fact]
    public void Reassign_ChangesOwnerAndReactivates()
    {
        var originalOwner = Guid.NewGuid();
        var newOwner = Guid.NewGuid();
        var token = DeviceToken.Register(originalOwner, "raw-token", "ios", DateTime.UtcNow);
        token.Revoke(DateTime.UtcNow);

        token.Reassign(newOwner, DateTime.UtcNow);

        Assert.Equal(newOwner, token.UserId);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void Touch_UpdatesLastUsedAtUtc()
    {
        var token = DeviceToken.Register(Guid.NewGuid(), "raw-token", "web", DateTime.UtcNow.AddDays(-1));
        var utcNow = DateTime.UtcNow;

        token.Touch(utcNow);

        Assert.Equal(utcNow, token.LastUsedAtUtc);
    }
}
