using SmartTaxi.Application.Support.Abstractions;
using SmartTaxi.Domain.Support.Entities;

namespace SmartTaxi.Application.Support;

/// <summary>
/// Concrete ISupportIncidentReporter. Idempotency is enforced by
/// ISupportIncidentRepository.TryAddAsync's underlying unique index on
/// (SourceType, SourceId): a losing concurrent TryAddAsync means another
/// caller already reported the same source event, so the pre-existing
/// incident's id is looked up and returned instead of failing the caller.
/// </summary>
public sealed class SupportIncidentReporter : ISupportIncidentReporter
{
    private readonly ISupportIncidentRepository _incidentRepository;

    public SupportIncidentReporter(ISupportIncidentRepository incidentRepository)
    {
        _incidentRepository = incidentRepository;
    }

    public async Task<Guid> ReportAsync(SupportIncidentReportRequest request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.SourceType) && request.SourceId is { } sourceId)
        {
            var existing = await _incidentRepository.GetBySourceAsync(request.SourceType, sourceId, cancellationToken);

            if (existing is not null)
            {
                return existing.Id;
            }
        }

        var utcNow = DateTime.UtcNow;
        var incident = SupportIncident.Create(
            request.Type, request.Severity, request.Title, request.Description, request.ReportedByUserId, request.RelatedEntityType,
            request.RelatedEntityId, request.Latitude, request.Longitude, request.OccurredAtUtc, request.SourceType, request.SourceId,
            utcNow);

        var added = await _incidentRepository.TryAddAsync(incident, cancellationToken);

        if (added)
        {
            return incident.Id;
        }

        var winner = await _incidentRepository.GetBySourceAsync(request.SourceType!, request.SourceId!.Value, cancellationToken);
        return winner!.Id;
    }
}
