using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class SharedRideParticipantTests
{
    [Fact]
    public void Create_Succeeds_WithPendingApprovalStatus()
    {
        var participant = new SharedRideParticipant(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.Equal(SharedRideParticipantApprovalStatus.Pending, participant.ApprovalStatus);
    }
}
