using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;

public sealed record RegenerateRecoveryCodesCommand(Guid UserId) : ICommand<Result<RegenerateRecoveryCodesResult>>;
