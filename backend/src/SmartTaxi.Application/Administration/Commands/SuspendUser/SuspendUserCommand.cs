using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Administration.Commands.SuspendUser;

public sealed record SuspendUserCommand(Guid TargetUserId, Guid ActingAdminUserId) : ICommand<Result>;
