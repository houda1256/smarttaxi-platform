using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.RevokeDeviceToken;

public sealed record RevokeDeviceTokenCommand(Guid DeviceTokenId, Guid RequestingUserId) : ICommand<Result>;
