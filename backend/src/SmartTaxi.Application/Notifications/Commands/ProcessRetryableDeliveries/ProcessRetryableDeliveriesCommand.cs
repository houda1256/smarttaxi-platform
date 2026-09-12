using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.ProcessRetryableDeliveries;

public sealed record ProcessRetryableDeliveriesCommand : ICommand<Result<int>>;
