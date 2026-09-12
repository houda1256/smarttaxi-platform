using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RequestPhoneVerification;

public sealed record RequestPhoneVerificationCommand(Guid UserId, string PhoneNumber) : ICommand<Result>;
