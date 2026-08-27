using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Administration.Commands.ResetUserTwoFactor;

public sealed record ResetUserTwoFactorCommand(Guid TargetUserId, Guid ActingAdminUserId) : ICommand<Result>;
