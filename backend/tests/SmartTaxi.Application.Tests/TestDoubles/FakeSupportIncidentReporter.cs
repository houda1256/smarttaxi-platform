using SmartTaxi.Application.Support.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Records every request for assertion, and simulates the real idempotency contract by returning the same id for a repeated (SourceType, SourceId).</summary>
public sealed class FakeSupportIncidentReporter : ISupportIncidentReporter
{
    private readonly Dictionary<(string SourceType, Guid SourceId), Guid> _idBySource = new();

    public List<SupportIncidentReportRequest> Requests { get; } = [];

    public Task<Guid> ReportAsync(SupportIncidentReportRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);

        if (request.SourceType is not null && request.SourceId is { } sourceId)
        {
            var key = (request.SourceType, sourceId);

            if (_idBySource.TryGetValue(key, out var existingId))
            {
                return Task.FromResult(existingId);
            }

            var newId = Guid.NewGuid();
            _idBySource[key] = newId;
            return Task.FromResult(newId);
        }

        return Task.FromResult(Guid.NewGuid());
    }
}
