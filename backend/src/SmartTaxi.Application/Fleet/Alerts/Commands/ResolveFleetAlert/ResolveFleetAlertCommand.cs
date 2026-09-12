using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Alerts.Commands.ResolveFleetAlert;

public sealed record ResolveFleetAlertCommand(Guid RequestingUserId, Guid AlertId) : ICommand<Result>;
