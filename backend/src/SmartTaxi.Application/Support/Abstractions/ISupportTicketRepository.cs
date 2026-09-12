using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Support.Entities;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Abstractions;

public interface ISupportTicketRepository
{
    Task<bool> TryAddAsync(SupportTicket ticket, CancellationToken cancellationToken);

    Task<SupportTicket?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken);

    Task<PagedResult<SupportTicket>> GetForRequesterAsync(Guid requesterUserId, int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<PagedResult<SupportTicket>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Open -&gt; Assigned, guarded WHERE Status==Open AND AssignedAdminUserId IS NULL — the initial-assignment race guard (two admins claiming the same unassigned ticket).</summary>
    Task<bool> TryAssignAsync(Guid ticketId, Guid adminUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Admin override, callable by any admin at any non-Closed status — never restricted to the currently assigned admin.</summary>
    Task<bool> TryReassignAsync(Guid ticketId, Guid newAdminUserId, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// The single atomic conditional transition for every pure-status change.
    /// requiredAdminUserId, when non-null, is added to the WHERE clause
    /// (Start/WaitingForCustomer/Resolve — only the assigned admin). For
    /// Close/Reopen (requester-or-any-admin allowed), the caller passes null
    /// here because the Application handler has already established
    /// authorization before calling this method — the only thing this atomic
    /// call still needs to guard against is the status race itself.
    /// </summary>
    Task<bool> TryTransitionAsync(
        Guid ticketId, IReadOnlyCollection<SupportTicketStatus> allowedFromStatuses, SupportTicketStatus newStatus,
        Guid? requiredAdminUserId, string? resolution, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>
    /// Atomic: inserts the requester's message AND, only if the ticket is
    /// currently WaitingForCustomer, transitions it to InProgress in the same
    /// transaction — never two independently committed writes (approved
    /// design choice). Guarded WHERE RequesterUserId == requesterUserId AND
    /// Status != Closed.
    /// </summary>
    Task<bool> TryAddRequesterMessageAndAdvanceAsync(
        Guid ticketId, Guid requesterUserId, string body, DateTime utcNow, CancellationToken cancellationToken);

    /// <summary>Guarded WHERE EscalatedIncidentId IS NULL — the idempotency anchor ISupportTicketEscalationRepository relies on.</summary>
    Task<bool> TrySetEscalatedIncidentIdAsync(Guid ticketId, Guid incidentId, DateTime utcNow, CancellationToken cancellationToken);
}
