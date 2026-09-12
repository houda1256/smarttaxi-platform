using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.RegisterDeviceToken;

public sealed record RegisterDeviceTokenCommand(Guid UserId, string Token, string Platform) : ICommand<Result<Guid>>;
