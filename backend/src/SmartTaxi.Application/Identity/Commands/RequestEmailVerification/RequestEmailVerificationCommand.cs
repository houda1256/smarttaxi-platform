using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RequestEmailVerification;

public sealed record RequestEmailVerificationCommand(string Email) : ICommand<Result>;
