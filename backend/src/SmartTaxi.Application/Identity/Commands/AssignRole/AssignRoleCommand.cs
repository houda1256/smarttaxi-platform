using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.AssignRole;

public sealed record AssignRoleCommand(Guid UserId, string Role, Guid ActingAdminUserId) : ICommand<Result<AssignRoleResult>>;
