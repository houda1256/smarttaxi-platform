using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetById;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Tests.Fleet.Fleets.Queries;

public class GetFleetByIdQueryHandlerTests
{
    private readonly FakeFleetRepository _fleetRepository = new();
    private readonly FakeFleetMemberRepository _memberRepository = new();
    private readonly GetFleetByIdQueryHandler _handler;

    public GetFleetByIdQueryHandlerTests()
    {
        _handler = new GetFleetByIdQueryHandler(_fleetRepository, _memberRepository);
    }

    [Fact]
    public async Task Handle_ForOwner_ReturnsFleet()
    {
        var ownerId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(new GetFleetByIdQuery(ownerId, fleet.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ForCollaborator_ReturnsFleet()
    {
        var ownerId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var fleet = FleetOrganization.Create(ownerId, "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);
        await _memberRepository.AddAsync(new FleetMember(fleet.Id, collaboratorId, FleetCollaboratorRole.Viewer, DateTime.UtcNow), CancellationToken.None);

        var result = await _handler.Handle(new GetFleetByIdQuery(collaboratorId, fleet.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ForUnrelatedUser_ReturnsNotFound()
    {
        var fleet = FleetOrganization.Create(Guid.NewGuid(), "Fleet", null, Guid.NewGuid(), DateTime.UtcNow);
        await _fleetRepository.AddAsync(fleet, CancellationToken.None);

        var result = await _handler.Handle(new GetFleetByIdQuery(Guid.NewGuid(), fleet.Id), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
