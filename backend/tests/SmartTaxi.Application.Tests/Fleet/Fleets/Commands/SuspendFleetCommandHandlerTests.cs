using SmartTaxi.Application.Fleet.Fleets.Commands.SuspendFleet;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Fleets.Commands;

public class SuspendFleetCommandHandlerTests
{
    private readonly FakeFleetRepository _repository = new();
    private readonly SuspendFleetCommandHandler _handler;

    public SuspendFleetCommandHandlerTests()
    {
        _handler = new SuspendFleetCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ForOwnActiveFleet_Suspends()
    {
        var ownerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(new SuspendFleetCommand(ownerId, fleet.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(fleet.Id, CancellationToken.None);
        Assert.Equal(FleetStatus.Suspended, reloaded!.Status);
    }

    [Fact]
    public async Task Handle_ForAnotherOwnersFleet_ReturnsNotFoundAndDoesNotSuspend()
    {
        var fleet = FleetOrganization.Create(Guid.NewGuid(), "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(new SuspendFleetCommand(Guid.NewGuid(), fleet.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(fleet.Id, CancellationToken.None);
        Assert.Equal(FleetStatus.Active, reloaded!.Status);
    }
}
