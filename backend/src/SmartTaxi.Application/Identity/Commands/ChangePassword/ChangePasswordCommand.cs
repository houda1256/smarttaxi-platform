using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ChangePassword;

public sealed record ChangePasswordCommand(Guid UserId, Guid CurrentSessionId, string CurrentPassword, string NewPassword)
    : ICommand<Result>;
