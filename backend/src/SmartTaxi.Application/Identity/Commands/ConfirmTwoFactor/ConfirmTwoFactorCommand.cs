using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;

public sealed record ConfirmTwoFactorCommand(Guid UserId, string Code) : ICommand<Result<ConfirmTwoFactorResult>>;
