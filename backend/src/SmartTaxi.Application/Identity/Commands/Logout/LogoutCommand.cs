using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.Logout;

public sealed record LogoutCommand(Guid SessionId) : ICommand<Result>;
