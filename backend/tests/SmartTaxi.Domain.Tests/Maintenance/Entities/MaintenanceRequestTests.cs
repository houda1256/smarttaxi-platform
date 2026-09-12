using SmartTaxi.Domain.Maintenance.Entities;
using SmartTaxi.Domain.Maintenance.Enums;
using SmartTaxi.Domain.Maintenance.Events;

namespace SmartTaxi.Domain.Tests.Maintenance.Entities;

public class MaintenanceRequestTests
{
    private static MaintenanceRequest CreateValid() =>
        MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Bruit suspect au freinage", DateTime.UtcNow);

    [Fact]
    public void Create_WithValidFields_StartsAtPendingGarageResponseAndRaisesEvent()
    {
        var request = CreateValid();

        Assert.Equal(MaintenanceRequestStatus.PendingGarageResponse, request.Status);
        var raised = Assert.Single(request.DomainEvents);
        Assert.IsType<MaintenanceRequestCreated>(raised);
    }

    [Fact]
    public void Create_WithEmptyVehicleId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRequest.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "desc", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyOwnerUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRequest.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "desc", DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithEmptyGarageUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "desc", DateTime.UtcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDescription_Throws(string description)
    {
        Assert.Throws<ArgumentException>(() =>
            MaintenanceRequest.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), description, DateTime.UtcNow));
    }
}
