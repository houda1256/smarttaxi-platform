using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Alerts.Commands.DismissFleetAlert;

public sealed record DismissFleetAlertCommand(Guid RequestingUserId, Guid AlertId) : ICommand<Result>;
