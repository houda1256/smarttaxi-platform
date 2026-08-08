using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(Guid UserId, string CurrentPassword, string Code) : ICommand<Result>;
