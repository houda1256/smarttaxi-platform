using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;
using SmartTaxi.Domain.Fleet.Fleets.Events;

namespace SmartTaxi.Domain.Tests.Fleet.Fleets.Entities;

public class FleetOrganizationTests
{
    [Fact]
    public void Create_SetsActiveStatusAndRaisesCreatedEvent()
    {
        var ownerId = Guid.NewGuid();
        var cityId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var fleet = FleetOrganization.Create(ownerId, "My Fleet", "desc", cityId, utcNow);

        Assert.Equal(ownerId, fleet.OwnerId);
        Assert.Equal(FleetStatus.Active, fleet.Status);
        Assert.Single(fleet.DomainEvents);
        var raised = Assert.IsType<FleetCreated>(fleet.DomainEvents.Single());
        Assert.Equal(fleet.Id, raised.FleetId);
    }

    [Fact]
    public void UpdateDetails_ReplacesNameDescriptionAndCity()
    {
        var fleet = FleetOrganization.Create(Guid.NewGuid(), "Old Name", null, Guid.NewGuid(), DateTime.UtcNow);
        var newCityId = Guid.NewGuid();

        fleet.UpdateDetails("New Name", "New desc", newCityId, DateTime.UtcNow);

        Assert.Equal("New Name", fleet.Name);
        Assert.Equal(newCityId, fleet.CityId);
    }
}
