using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RevokeSession;

public sealed record RevokeSessionCommand(Guid UserId, Guid SessionId) : ICommand<Result>;
