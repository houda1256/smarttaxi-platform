using SmartTaxi.Domain.RoadsideAssistance.Entities;
using SmartTaxi.Domain.RoadsideAssistance.Enums;
using SmartTaxi.Domain.RoadsideAssistance.Events;

namespace SmartTaxi.Domain.Tests.RoadsideAssistance.Entities;

public class RoadsideAssistanceRequestTests
{
    private static RoadsideAssistanceRequest CreateValid() =>
        RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.TaxiOwner, Guid.NewGuid(), null, RoadsideServiceType.Towing, RoadsideUrgency.High,
            "Panne moteur sur autoroute", 36.8, 10.18, "Autoroute A1", "Tunis", DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_StartsAtPartnersAvailableAndRaisesEvent()
    {
        var request = CreateValid();

        Assert.Equal(RoadsideRequestStatus.PartnersAvailable, request.Status);
        var raised = Assert.Single(request.DomainEvents);
        Assert.IsType<RoadsideAssistanceRequested>(raised);
        Assert.Equal("TUNIS", request.City);
        Assert.Null(request.SelectedPartnerUserId);
    }

    [Fact]
    public void Create_WithEmptyRequesterUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsideAssistanceRequest.Create(
                Guid.Empty, RoadsideRequesterRole.Driver, Guid.NewGuid(), null, RoadsideServiceType.FuelDelivery, RoadsideUrgency.Low,
                "desc", 36.8, 10.18, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyVehicleId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsideAssistanceRequest.Create(
                Guid.NewGuid(), RoadsideRequesterRole.Driver, Guid.Empty, null, RoadsideServiceType.FuelDelivery, RoadsideUrgency.Low,
                "desc", 36.8, 10.18, null, null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDescription_Throws(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsideAssistanceRequest.Create(
                Guid.NewGuid(), RoadsideRequesterRole.Driver, Guid.NewGuid(), null, RoadsideServiceType.FuelDelivery, RoadsideUrgency.Low,
                description, 36.8, 10.18, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithOutOfRangeCoordinates_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            RoadsideAssistanceRequest.Create(
                Guid.NewGuid(), RoadsideRequesterRole.Driver, Guid.NewGuid(), null, RoadsideServiceType.FuelDelivery, RoadsideUrgency.Low,
                "desc", 200, 10.18, null, null, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithRideId_KeepsItNullable()
    {
        var rideId = Guid.NewGuid();
        var request = RoadsideAssistanceRequest.Create(
            Guid.NewGuid(), RoadsideRequesterRole.Driver, Guid.NewGuid(), rideId, RoadsideServiceType.BatteryJumpStart,
            RoadsideUrgency.Medium, "desc", 36.8, 10.18, null, null, DateTime.UtcNow);

        Assert.Equal(rideId, request.RideId);
    }
}
