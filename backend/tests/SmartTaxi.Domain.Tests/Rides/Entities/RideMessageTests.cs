using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideMessageTests
{
    [Fact]
    public void Create_Succeeds_WithProvidedContent()
    {
        var message = new RideMessage(Guid.NewGuid(), Guid.NewGuid(), RideMessageType.Text, "On my way", DateTime.UtcNow);

        Assert.Equal("On my way", message.Content);
        Assert.False(message.IsReported);
    }

    [Fact]
    public void Create_WithEmptyContent_Throws()
    {
        Assert.Throws<ArgumentException>(() => new RideMessage(Guid.NewGuid(), Guid.NewGuid(), RideMessageType.Text, "", DateTime.UtcNow));
    }
}
