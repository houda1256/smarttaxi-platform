using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Domain.Tests.Rides.Entities;

public class RideComplaintTests
{
    [Fact]
    public void Submit_Succeeds_WithOpenStatus()
    {
        var complaint = RideComplaint.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RideComplaintCategory.DriverBehavior, "Rude driver", DateTime.UtcNow);

        Assert.Equal(RideComplaintStatus.Open, complaint.Status);
        Assert.Single(complaint.DomainEvents);
    }

    [Fact]
    public void Submit_WithSameComplainantAndConcerned_Throws()
    {
        var userId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => RideComplaint.Submit(
            Guid.NewGuid(), userId, userId, RideComplaintCategory.Other, "Description", DateTime.UtcNow));
    }

    [Fact]
    public void Submit_WithEmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() => RideComplaint.Submit(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), RideComplaintCategory.Other, "", DateTime.UtcNow));
    }
}
