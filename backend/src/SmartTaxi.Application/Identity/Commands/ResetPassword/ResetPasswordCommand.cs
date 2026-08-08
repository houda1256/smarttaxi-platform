using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string RawToken, string NewPassword) : ICommand<Result>;
