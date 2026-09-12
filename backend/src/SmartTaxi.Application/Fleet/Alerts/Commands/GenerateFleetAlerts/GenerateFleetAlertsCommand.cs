using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Alerts.Commands.GenerateFleetAlerts;

public sealed record GenerateFleetAlertsCommand(Guid OwnerId) : ICommand<Result<int>>;
