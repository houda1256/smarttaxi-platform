using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RevokeAllSessions;

public sealed record RevokeAllSessionsCommand(Guid UserId) : ICommand<Result>;
