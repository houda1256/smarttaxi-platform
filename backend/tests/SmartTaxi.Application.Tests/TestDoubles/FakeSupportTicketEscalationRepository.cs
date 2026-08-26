using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeSupportTicketEscalationRepository : ISupportTicketEscalationRepository
{
    public Guid? TicketIdToEscalate { get; set; }
    public Guid? ExistingEscalatedIncidentId { get; set; }
    public bool TicketExists { get; set; } = true;

    public Task<Guid?> TryEscalateAsync(
        Guid ticketId, Guid adminUserId, SupportIncidentType incidentType, SupportIncidentSeverity severity, string? titleOverride,
        DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!TicketExists)
        {
            return Task.FromResult<Guid?>(null);
        }

        TicketIdToEscalate = ticketId;
        var incidentId = ExistingEscalatedIncidentId ?? Guid.NewGuid();
        ExistingEscalatedIncidentId = incidentId;
        return Task.FromResult<Guid?>(incidentId);
    }
}
