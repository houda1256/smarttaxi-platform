using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ConfirmEmailVerification;

public sealed record ConfirmEmailVerificationCommand(string RawToken) : ICommand<Result>;
