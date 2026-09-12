using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Domain.Fleet.Fleets.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Fleet.Repositories;

internal sealed class FleetMemberRepository : IFleetMemberRepository
{
    private readonly ApplicationDbContext _context;

    public FleetMemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FleetMember member, CancellationToken cancellationToken)
    {
        await _context.FleetMembers.AddAsync(member, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<FleetMember?> GetByIdAsync(Guid memberId, CancellationToken cancellationToken) =>
        _context.FleetMembers.FirstOrDefaultAsync(member => member.Id == memberId, cancellationToken);

    public Task<FleetMember?> GetMembershipAsync(Guid fleetId, Guid userId, CancellationToken cancellationToken) =>
        _context.FleetMembers.FirstOrDefaultAsync(
            member => member.FleetId == fleetId && member.UserId == userId, cancellationToken);

    public async Task<IReadOnlyCollection<FleetMember>> GetForFleetAsync(Guid fleetId, CancellationToken cancellationToken) =>
        await _context.FleetMembers.Where(member => member.FleetId == fleetId).ToListAsync(cancellationToken);

    public async Task RemoveAsync(Guid memberId, CancellationToken cancellationToken)
    {
        await _context.FleetMembers.Where(member => member.Id == memberId).ExecuteDeleteAsync(cancellationToken);
    }
}
