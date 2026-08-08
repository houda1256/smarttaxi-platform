using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;

namespace SmartTaxi.Application.Identity.Commands.RegenerateRecoveryCodes;

public sealed class RegenerateRecoveryCodesCommandHandler
    : ICommandHandler<RegenerateRecoveryCodesCommand, Result<RegenerateRecoveryCodesResult>>
{
    private const string NotEnabledError = "La double authentification n'est pas activée.";

    private readonly IUserRepository _userRepository;
    private readonly RecoveryCodeService _recoveryCodeService;

    public RegenerateRecoveryCodesCommandHandler(IUserRepository userRepository, RecoveryCodeService recoveryCodeService)
    {
        _userRepository = userRepository;
        _recoveryCodeService = recoveryCodeService;
    }

    public async Task<Result<RegenerateRecoveryCodesResult>> Handle(
        RegenerateRecoveryCodesCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive || !user.TwoFactorEnabled)
        {
            return Result<RegenerateRecoveryCodesResult>.Failure(NotEnabledError, ErrorType.Validation);
        }

        var recoveryCodes = await _recoveryCodeService.IssueNewBatchAsync(user.Id, DateTime.UtcNow, cancellationToken);

        return Result<RegenerateRecoveryCodesResult>.Success(new RegenerateRecoveryCodesResult(recoveryCodes));
    }
}
