using SmartTaxi.Domain.Fleet.Fleets.Entities;

namespace SmartTaxi.Application.Fleet.Common.Abstractions;

public interface IFleetMemberRepository
{
    Task AddAsync(FleetMember member, CancellationToken cancellationToken);

    Task<FleetMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken);

    Task<FleetMember?> GetMembershipAsync(Guid fleetId, Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FleetMember>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken);

    Task RemoveAsync(Guid memberId, CancellationToken cancellationToken);
}
