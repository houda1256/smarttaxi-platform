using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Support.Repositories;

internal sealed class SupportIncidentRepository : ISupportIncidentRepository
{
    private readonly ApplicationDbContext _context;

    public SupportIncidentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(SupportIncident incident, CancellationToken cancellationToken)
    {
        await _context.SupportIncidents.AddAsync(incident, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(incident).State = EntityState.Detached;
            return false;
        }
    }

    public Task<SupportIncident?> GetByIdAsync(Guid incidentId, CancellationToken cancellationToken) =>
        _context.SupportIncidents.FirstOrDefaultAsync(incident => incident.Id == incidentId, cancellationToken);

    public Task<SupportIncident?> GetBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken) =>
        _context.SupportIncidents.FirstOrDefaultAsync(
            incident => incident.SourceType == sourceType && incident.SourceId == sourceId, cancellationToken);

    public async Task<PagedResult<SupportIncident>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.SupportIncidents.CountAsync(cancellationToken);

        var items = await _context.SupportIncidents
            .OrderByDescending(incident => incident.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SupportIncident>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TryAcknowledgeAsync(Guid incidentId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportIncidents
            .Where(incident => incident.Id == incidentId && incident.Status == SupportIncidentStatus.Reported)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(incident => incident.Status, SupportIncidentStatus.Acknowledged)
                .SetProperty(incident => incident.AssignedAdminUserId, adminUserId)
                .SetProperty(incident => incident.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryReassignAsync(Guid incidentId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportIncidents
            .Where(incident => incident.Id == incidentId
                && incident.Status != SupportIncidentStatus.Closed && incident.Status != SupportIncidentStatus.FalsePositive)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(incident => incident.AssignedAdminUserId, newAdminUserId)
                .SetProperty(incident => incident.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryTransitionAsync(
        Guid incidentId, IReadOnlyCollection<SupportIncidentStatus> allowedFromStatuses, SupportIncidentStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.SupportIncidents
            .Where(incident => incident.Id == incidentId && allowedFromStatuses.Contains(incident.Status)
                && (requiredAdminUserId == null || incident.AssignedAdminUserId == requiredAdminUserId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(incident => incident.Status, newStatus)
                .SetProperty(incident => incident.UpdatedAtUtc, utcNow)
                .SetProperty(
                    incident => incident.Resolution,
                    incident => newStatus == SupportIncidentStatus.Resolved ? resolution : incident.Resolution)
                .SetProperty(
                    incident => incident.ResolvedAtUtc,
                    incident => newStatus == SupportIncidentStatus.Resolved ? utcNow : incident.ResolvedAtUtc)
                .SetProperty(
                    incident => incident.ClosedAtUtc,
                    incident => newStatus == SupportIncidentStatus.Closed ? utcNow : incident.ClosedAtUtc),
                cancellationToken);

        return rows == 1;
    }
}
