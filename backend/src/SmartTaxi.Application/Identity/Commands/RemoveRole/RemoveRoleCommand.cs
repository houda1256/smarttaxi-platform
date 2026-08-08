using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RemoveRole;

public sealed record RemoveRoleCommand(Guid UserId, string Role) : ICommand<Result>;
