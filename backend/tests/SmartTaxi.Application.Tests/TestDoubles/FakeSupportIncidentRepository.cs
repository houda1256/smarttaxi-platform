using SmartTaxi.Application.Common;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>In-memory approximation of SupportIncidentRepository's atomic conditional updates, including the partial-unique-index-on-(SourceType,SourceId) idempotency guard simulated in TryAddAsync.</summary>
public sealed class FakeSupportIncidentRepository : ISupportIncidentRepository
{
    private readonly Dictionary<Guid, SupportIncident> _incidents = new();

    public Task<bool> TryAddAsync(SupportIncident incident, CancellationToken cancellationToken)
    {
        if (incident.SourceType is not null && incident.SourceId is not null
            && _incidents.Values.Any(i => i.SourceType == incident.SourceType && i.SourceId == incident.SourceId))
        {
            return Task.FromResult(false);
        }

        _incidents[incident.Id] = incident;
        return Task.FromResult(true);
    }

    public Task<SupportIncident?> GetByIdAsync(Guid incidentId, CancellationToken cancellationToken) =>
        Task.FromResult(_incidents.GetValueOrDefault(incidentId));

    public Task<SupportIncident?> GetBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken)
    {
        var incident = _incidents.Values.FirstOrDefault(i => i.SourceType == sourceType && i.SourceId == sourceId);
        return Task.FromResult(incident);
    }

    public Task<PagedResult<SupportIncident>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var items = _incidents.Values.ToList();
        return Task.FromResult(new PagedResult<SupportIncident>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TryAcknowledgeAsync(Guid incidentId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || incident.Status != SupportIncidentStatus.Reported)
        {
            return Task.FromResult(false);
        }

        SetProperty(incident, nameof(SupportIncident.Status), SupportIncidentStatus.Acknowledged);
        SetProperty(incident, nameof(SupportIncident.AssignedAdminUserId), adminUserId);
        SetProperty(incident, nameof(SupportIncident.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryReassignAsync(Guid incidentId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident)
            || incident.Status is SupportIncidentStatus.Closed or SupportIncidentStatus.FalsePositive)
        {
            return Task.FromResult(false);
        }

        SetProperty(incident, nameof(SupportIncident.AssignedAdminUserId), newAdminUserId);
        SetProperty(incident, nameof(SupportIncident.UpdatedAtUtc), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryTransitionAsync(
        Guid incidentId, IReadOnlyCollection<SupportIncidentStatus> allowedFromStatuses, SupportIncidentStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_incidents.TryGetValue(incidentId, out var incident) || !allowedFromStatuses.Contains(incident.Status)
            || (requiredAdminUserId is not null && incident.AssignedAdminUserId != requiredAdminUserId))
        {
            return Task.FromResult(false);
        }

        SetProperty(incident, nameof(SupportIncident.Status), newStatus);
        SetProperty(incident, nameof(SupportIncident.UpdatedAtUtc), utcNow);

        switch (newStatus)
        {
            case SupportIncidentStatus.Resolved:
                SetProperty(incident, nameof(SupportIncident.Resolution), resolution);
                SetProperty(incident, nameof(SupportIncident.ResolvedAtUtc), utcNow);
                break;
            case SupportIncidentStatus.Closed:
                SetProperty(incident, nameof(SupportIncident.ClosedAtUtc), utcNow);
                break;
        }

        return Task.FromResult(true);
    }

    private static void SetProperty(SupportIncident incident, string propertyName, object? value) =>
        typeof(SupportIncident).GetProperty(propertyName)!.SetValue(incident, value);
}
