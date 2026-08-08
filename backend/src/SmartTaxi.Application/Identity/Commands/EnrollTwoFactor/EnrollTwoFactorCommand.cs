using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.EnrollTwoFactor;

public sealed record EnrollTwoFactorCommand(Guid UserId) : ICommand<Result<EnrollTwoFactorResult>>;
