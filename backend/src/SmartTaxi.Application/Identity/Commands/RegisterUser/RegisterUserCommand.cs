using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RegisterUser;

public sealed record RegisterUserCommand(string Email, string Password)
    : ICommand<Result<RegisterUserResult>>;
