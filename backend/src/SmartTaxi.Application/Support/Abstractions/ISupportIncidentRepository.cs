using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Abstractions;

public interface ISupportIncidentRepository
{
    /// <summary>Returns false if the partial unique index on (SourceType, SourceId) rejects a concurrent duplicate for the same source event — the real, race-safe enforcement point for ISupportIncidentReporter's idempotency guarantee.</summary>
    Task<bool> TryAddAsync(SupportIncident incident, CancellationToken cancellationToken);

    Task<SupportIncident?> GetByIdAsync(Guid incidentId, CancellationToken cancellationToken);

    Task<SupportIncident?> GetBySourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken);

    Task<PagedResult<SupportIncident>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Reported -&gt; Acknowledged, folding in first assignment (sets AssignedAdminUserId to the caller) — guarded WHERE Status==Reported, so AssignedAdminUserId is always still null at this point by construction.</summary>
    Task<bool> TryAcknowledgeAsync(Guid incidentId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Admin override, callable by any admin at any non-terminal status.</summary>
    Task<bool> TryReassignAsync(Guid incidentId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>The single atomic conditional transition for Investigating/Resolved/Closed/Reopened/FalsePositive — same requiredAdminUserId convention as ISupportTicketRepository.TryTransitionAsync.</summary>
    Task<bool> TryTransitionAsync(
        Guid incidentId, IReadOnlyCollection<SupportIncidentStatus> allowedFromStatuses, SupportIncidentStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken);
}
