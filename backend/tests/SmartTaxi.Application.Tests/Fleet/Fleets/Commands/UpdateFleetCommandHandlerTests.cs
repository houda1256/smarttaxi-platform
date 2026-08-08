using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Fleets.Commands.UpdateFleet;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Tests.Fleet.Fleets.Commands;

public class UpdateFleetCommandHandlerTests
{
    private readonly FakeFleetRepository _repository = new();
    private readonly UpdateFleetCommandHandler _handler;

    public UpdateFleetCommandHandlerTests()
    {
        _handler = new UpdateFleetCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ForOwnFleet_UpdatesDetails()
    {
        var ownerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Old", null, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(
            new UpdateFleetCommand(ownerId, fleet.Id, "New", "desc", Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var reloaded = await _repository.GetByIdAsync(fleet.Id, CancellationToken.None);
        Assert.Equal("New", reloaded!.Name);
    }

    [Fact]
    public async Task Handle_ForAnotherOwnersFleet_ReturnsNotFound()
    {
        var fleet = FleetOrganization.Create(Guid.NewGuid(), "Old", null, Guid.NewGuid(), DateTime.UtcNow);
        await _repository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(
            new UpdateFleetCommand(Guid.NewGuid(), fleet.Id, "Hacked", null, Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        var reloaded = await _repository.GetByIdAsync(fleet.Id, CancellationToken.None);
        Assert.Equal("Old", reloaded!.Name);
    }
}
