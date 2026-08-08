using SmartTaxi.Application.Rides.Abstractions;
using SmartTaxi.Domain.Rides.Entities;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideSafetyEventRepository : IRideSafetyEventRepository
{
    private readonly Dictionary<Guid, RideSafetyEvent> _eventsById = new();

    public Task AddAsync(RideSafetyEvent safetyEvent, CancellationToken cancellationToken)
    {
        _eventsById[safetyEvent.Id] = safetyEvent;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<RideSafetyEvent>> GetForRideAsync(Guid rideId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<RideSafetyEvent> events = _eventsById.Values.Where(e => e.RideId == rideId).ToList();
        return Task.FromResult(events);
    }

    public Task<bool> TryAcknowledgeAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(safetyEventId, RideSafetyEventStatus.Open, RideSafetyEventStatus.Acknowledged);

    public Task<bool> TryResolveAsync(Guid safetyEventId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_eventsById.TryGetValue(safetyEventId, out var safetyEvent) || safetyEvent.Status == RideSafetyEventStatus.Resolved)
        {
            return Task.FromResult(false);
        }

        typeof(RideSafetyEvent).GetProperty(nameof(RideSafetyEvent.Status))!.SetValue(safetyEvent, RideSafetyEventStatus.Resolved);
        return Task.FromResult(true);
    }

    private Task<bool> TryTransition(Guid safetyEventId, RideSafetyEventStatus from, RideSafetyEventStatus to)
    {
        if (!_eventsById.TryGetValue(safetyEventId, out var safetyEvent) || safetyEvent.Status != from)
        {
            return Task.FromResult(false);
        }

        typeof(RideSafetyEvent).GetProperty(nameof(RideSafetyEvent.Status))!.SetValue(safetyEvent, to);
        return Task.FromResult(true);
    }
}
