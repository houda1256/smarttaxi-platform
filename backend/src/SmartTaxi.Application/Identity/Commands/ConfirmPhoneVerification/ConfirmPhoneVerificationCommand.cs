using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ConfirmPhoneVerification;

public sealed record ConfirmPhoneVerificationCommand(Guid UserId, string Otp) : ICommand<Result>;
