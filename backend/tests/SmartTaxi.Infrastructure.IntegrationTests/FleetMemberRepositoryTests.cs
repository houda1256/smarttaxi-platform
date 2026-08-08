using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Domain.Fleet.Fleets.Enums;
using SmartTaxi.Infrastructure.Fleet.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class FleetMemberRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public FleetMemberRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetMembershipAsync_ForUserNotAddedToFleet_ReturnsNull_CrossFleetDenial()
    {
        var fleetId = Guid.NewGuid();
        var memberUserId = Guid.NewGuid();
        var outsiderUserId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var fleet = FleetOrganization.Create(Guid.NewGuid(), "Fleet A", null, Guid.NewGuid(), DateTime.UtcNow);
            await writeContext.Fleets.AddAsync(fleet, CancellationToken.None);
            await writeContext.SaveChangesAsync(CancellationToken.None);

            var member = new FleetMember(fleet.Id, memberUserId, FleetCollaboratorRole.Dispatcher, DateTime.UtcNow);
            await new FleetMemberRepository(writeContext).AddAsync(member, CancellationToken.None);

            fleetId = fleet.Id;
        }

        await using var readContext = _fixture.CreateContext();
        var repository = new FleetMemberRepository(readContext);

        var actualMembership = await repository.GetMembershipAsync(fleetId, memberUserId, CancellationToken.None);
        var outsiderMembership = await repository.GetMembershipAsync(fleetId, outsiderUserId, CancellationToken.None);

        Assert.NotNull(actualMembership);
        Assert.Null(outsiderMembership);
    }

    [Fact]
    public async Task ConcurrentAddAsync_WithSameFleetAndUser_OnlyOneSucceeds_EnforcedByDbUniqueIndex()
    {
        var fleetId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using (var writeContext = _fixture.CreateContext())
        {
            var fleet = FleetOrganization.Create(Guid.NewGuid(), "Fleet B", null, Guid.NewGuid(), DateTime.UtcNow);
            await writeContext.Fleets.AddAsync(fleet, CancellationToken.None);
            await writeContext.SaveChangesAsync(CancellationToken.None);
            fleetId = fleet.Id;
        }

        var memberA = new FleetMember(fleetId, userId, FleetCollaboratorRole.Accountant, DateTime.UtcNow);
        var memberB = new FleetMember(fleetId, userId, FleetCollaboratorRole.Viewer, DateTime.UtcNow);

        await using var contextA = _fixture.CreateContext();
        await using var contextB = _fixture.CreateContext();

        var results = await Task.WhenAll(
            TryAddAsync(new FleetMemberRepository(contextA), memberA),
            TryAddAsync(new FleetMemberRepository(contextB), memberB));

        Assert.Single(results, succeeded => succeeded);

        await using var readContext = _fixture.CreateContext();
        var count = await readContext.FleetMembers.CountAsync(m => m.FleetId == fleetId && m.UserId == userId);
        Assert.Equal(1, count);
    }

    private static async Task<bool> TryAddAsync(FleetMemberRepository repository, FleetMember member)
    {
        try
        {
            await repository.AddAsync(member, CancellationToken.None);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            return false;
        }
    }
}
