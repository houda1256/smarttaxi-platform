using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Administration.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(Guid TargetUserId, Guid ActingAdminUserId) : ICommand<Result>;
