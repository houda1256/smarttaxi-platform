using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideConversationTests
{
    [Fact]
    public void CreateForRide_Succeeds_WithActiveStatus()
    {
        var conversation = RideConversation.CreateForRide(Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(RideConversationStatus.Active, conversation.Status);
    }
}
