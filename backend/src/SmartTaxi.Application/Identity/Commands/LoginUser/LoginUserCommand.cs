using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.LoginUser;

public sealed record LoginUserCommand(string Email, string Password)
    : ICommand<Result<LoginUserResult>>;
