using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand<Result>;
