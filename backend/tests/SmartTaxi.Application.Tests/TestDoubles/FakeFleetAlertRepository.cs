using SmartTaxi.Application.Fleet.Alerts.Abstractions;
using SmartTaxi.Domain.Fleet.Alerts.Entities;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFleetAlertRepository : IFleetAlertRepository
{
    private readonly Dictionary<Guid, FleetAlert> _alertsById = new();

    public IReadOnlyCollection<FleetAlert> Alerts => _alertsById.Values.ToList();

    public Task AddAsync(FleetAlert alert, CancellationToken cancellationToken)
    {
        _alertsById[alert.Id] = alert;
        return Task.CompletedTask;
    }

    public Task<FleetAlert?> GetByIdAsync(Guid alertId, CancellationToken cancellationToken) =>
        Task.FromResult(_alertsById.GetValueOrDefault(alertId));

    public Task<IReadOnlyCollection<FleetAlert>> GetForOwnerAsync(
        Guid ownerId, FleetAlertStatus? status, CancellationToken cancellationToken)
    {
        var query = _alertsById.Values.Where(a => a.OwnerId == ownerId);

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        IReadOnlyCollection<FleetAlert> alerts = query.ToList();
        return Task.FromResult(alerts);
    }

    public Task<bool> ExistsOpenAlertAsync(
        Guid ownerId, FleetAlertType alertType, Guid? relatedEntityId, CancellationToken cancellationToken)
    {
        var exists = _alertsById.Values.Any(a =>
            a.OwnerId == ownerId && a.AlertType == alertType && a.RelatedEntityId == relatedEntityId
            && a.Status == FleetAlertStatus.Open);
        return Task.FromResult(exists);
    }

    public Task<bool> TryResolveAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(alertId, FleetAlertStatus.Resolved, utcNow);

    public Task<bool> TryDismissAsync(Guid alertId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(alertId, FleetAlertStatus.Dismissed, utcNow);

    private Task<bool> TryTransition(Guid alertId, FleetAlertStatus to, DateTime utcNow)
    {
        if (!_alertsById.TryGetValue(alertId, out var alert) || alert.Status != FleetAlertStatus.Open)
        {
            return Task.FromResult(false);
        }

        typeof(FleetAlert).GetProperty(nameof(FleetAlert.Status))!.SetValue(alert, to);
        typeof(FleetAlert).GetProperty(nameof(FleetAlert.ResolvedAt))!.SetValue(alert, utcNow);
        return Task.FromResult(true);
    }
}
