using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Administration.Commands.RevokeUserSessions;

public sealed record RevokeUserSessionsCommand(Guid TargetUserId, Guid ActingAdminUserId) : ICommand<Result<int>>;
