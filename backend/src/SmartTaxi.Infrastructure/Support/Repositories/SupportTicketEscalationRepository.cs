using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Support.Repositories;

/// <summary>
/// Mandatory atomicity design (approved plan, §J): one transaction guards
/// SupportTicket.EscalatedIncidentId IS NULL, creates the SupportIncident via
/// ISupportIncidentReporter (SourceType="SupportTicket", SourceId=ticket.Id —
/// so the incident-side idempotency guard also protects this path), and sets
/// EscalatedIncidentId in the same transaction — same shape as
/// RoadsideEscalationRepository. ISupportIncidentReporter's own
/// TryAddAsync/SaveChangesAsync call runs against the same ApplicationDbContext
/// instance, so it enlists in this ambient transaction rather than starting
/// its own.
/// </summary>
internal sealed class SupportTicketEscalationRepository : ISupportTicketEscalationRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ISupportIncidentReporter _incidentReporter;

    public SupportTicketEscalationRepository(ApplicationDbContext context, ISupportIncidentReporter incidentReporter)
    {
        _context = context;
        _incidentReporter = incidentReporter;
    }

    public async Task<Guid?> TryEscalateAsync(
        Guid ticketId, Guid adminUserId, SupportIncidentType incidentType, SupportIncidentSeverity severity, string? titleOverride,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var ticket = await _context.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        if (ticket.EscalatedIncidentId is { } existingIncidentId)
        {
            await transaction.RollbackAsync(cancellationToken);
            return existingIncidentId;
        }

        var title = string.IsNullOrWhiteSpace(titleOverride)
            ? $"Incident escaladé depuis le ticket {ticket.TicketNumber}"
            : titleOverride;

        var incidentId = await _incidentReporter.ReportAsync(
            new SupportIncidentReportRequest(
                incidentType, severity, title, ticket.Description, adminUserId, ticket.RelatedEntityType, ticket.RelatedEntityId,
                Latitude: null, Longitude: null, utcNow, SourceType: "SupportTicket", SourceId: ticket.Id),
            cancellationToken);

        var rows = await _context.SupportTickets
            .Where(t => t.Id == ticketId && t.EscalatedIncidentId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(t => t.EscalatedIncidentId, incidentId)
                .SetProperty(t => t.UpdatedAtUtc, utcNow), cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return incidentId;
        }

        await transaction.CommitAsync(cancellationToken);
        return incidentId;
    }
}
