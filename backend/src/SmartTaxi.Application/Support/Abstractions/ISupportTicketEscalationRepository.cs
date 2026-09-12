using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Support.Abstractions;

/// <summary>
/// Mandatory atomicity design (approved plan, §J): one transaction guards
/// SupportTicket.EscalatedIncidentId IS NULL, creates the SupportIncident via
/// ISupportIncidentReporter (SourceType="SupportTicket", SourceId=ticket.Id —
/// so the incident-side idempotency guard also protects this path), and sets
/// EscalatedIncidentId in the same transaction. A retry after a transient
/// failure re-checks the guard, so no duplicate incident is ever created.
/// </summary>
public interface ISupportTicketEscalationRepository
{
    /// <summary>Returns the resulting incident id (new or pre-existing) — null only if the ticket itself does not exist.</summary>
    Task<Guid?> TryEscalateAsync(
        Guid ticketId, Guid adminUserId, SupportIncidentType incidentType, SupportIncidentSeverity severity, string? titleOverride,
        DateTime utcNow, CancellationToken cancellationToken);
}
