using SmartTaxi.Domain.RoadsideAssistance.Enums;

namespace SmartTaxi.Domain.Tests.RoadsideAssistance.Enums;

public class RoadsideRequestStatusExtensionsTests
{
    [Theory]
    [InlineData(RoadsideRequestStatus.Completed, true)]
    [InlineData(RoadsideRequestStatus.Cancelled, true)]
    [InlineData(RoadsideRequestStatus.Expired, true)]
    [InlineData(RoadsideRequestStatus.Disputed, true)]
    [InlineData(RoadsideRequestStatus.Rejected, false)]
    [InlineData(RoadsideRequestStatus.PartnersAvailable, false)]
    [InlineData(RoadsideRequestStatus.InProgress, false)]
    public void IsTerminal_ReturnsExpectedValue(RoadsideRequestStatus status, bool expected)
    {
        Assert.Equal(expected, status.IsTerminal());
    }
}
