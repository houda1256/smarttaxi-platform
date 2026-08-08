using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFleetMemberRepository : IFleetMemberRepository
{
    private readonly Dictionary<Guid, FleetMember> _membersById = new();

    public Task AddAsync(FleetMember member, CancellationToken cancellationToken)
    {
        _membersById[member.Id] = member;
        return Task.CompletedTask;
    }

    public Task<FleetMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken) =>
        Task.FromResult(_membersById.GetValueOrDefault(memberId));

    public Task<FleetMember?> GetMembershipAsync(Guid fleetId, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(_membersById.Values.FirstOrDefault(m => m.FleetId == fleetId && m.UserId == userId));

    public Task<IReadOnlyCollection<FleetMember>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FleetMember> members = _membersById.Values.Where(m => m.FleetId == fleetId).ToList();
        return Task.FromResult(members);
    }

    public Task RemoveAsync(Guid memberId, CancellationToken cancellationToken)
    {
        _membersById.Remove(memberId);
        return Task.CompletedTask;
    }

    public int Count => _membersById.Count;
}
