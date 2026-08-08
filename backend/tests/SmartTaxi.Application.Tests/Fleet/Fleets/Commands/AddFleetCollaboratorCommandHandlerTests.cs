using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Fleets.Commands.AddFleetCollaborator;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Fleets.Commands;

public class AddFleetCollaboratorCommandHandlerTests
{
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly FakeFleetMemberRepository _memberRepository = new();
    private readonly AddFleetCollaboratorCommandHandler _handler;

    public AddFleetCollaboratorCommandHandlerTests()
    {
        _handler = new AddFleetCollaboratorCommandHandler(_fleetRepository, _memberRepository);
    }

    [Fact]
    public async Task Handle_ForOwnFleet_AddsCollaborator()
    {
        var ownerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);
        var collaboratorId = Guid.NewGuid();

        var result = await _handler.Handle(
            new AddFleetCollaboratorCommand(ownerId, fleet.Id, collaboratorId, FleetCollaboratorRole.Dispatcher),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _memberRepository.Count);
    }

    [Fact]
    public async Task Handle_ForAnotherOwnersFleet_ReturnsNotFound()
    {
        var fleet = FleetOrganization.Create(Guid.NewGuid(), "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(
            new AddFleetCollaboratorCommand(Guid.NewGuid(), fleet.Id, Guid.NewGuid(), FleetCollaboratorRole.Viewer),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
        Assert.Equal(0, _memberRepository.Count);
    }

    [Fact]
    public async Task Handle_WhenAlreadyMember_ReturnsConflict()
    {
        var ownerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);
        var collaboratorId = Guid.NewGuid();
        await _handler.Handle(
            new AddFleetCollaboratorCommand(ownerId, fleet.Id, collaboratorId, FleetCollaboratorRole.Viewer), CancellationToken.None);

        var result = await _handler.Handle(
            new AddFleetCollaboratorCommand(ownerId, fleet.Id, collaboratorId, FleetCollaboratorRole.Accountant), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
