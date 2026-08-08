using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.Application.Fleet.Alerts.Queries.GetFleetAlerts;

public sealed record GetFleetAlertsQuery(Guid OwnerId, FleetAlertStatus? Status) : IQuery<IReadOnlyCollection<FleetAlertSummary>>;
