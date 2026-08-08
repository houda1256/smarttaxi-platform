using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;

namespace SmartTaxi.Application.Identity.Commands.ConfirmTwoFactor;

public sealed class ConfirmTwoFactorCommandHandler : ICommandHandler<ConfirmTwoFactorCommand, Result<ConfirmTwoFactorResult>>
{
    private const string InvalidError = "Le code est invalide ou aucune inscription n'est en cours.";

    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorSecretProtector _protector;
    private readonly RecoveryCodeService _recoveryCodeService;

    public ConfirmTwoFactorCommandHandler(
        IUserRepository userRepository,
        ITotpService totpService,
        ITwoFactorSecretProtector protector,
        RecoveryCodeService recoveryCodeService)
    {
        _userRepository = userRepository;
        _totpService = totpService;
        _protector = protector;
        _recoveryCodeService = recoveryCodeService;
    }

    public async Task<Result<ConfirmTwoFactorResult>> Handle(ConfirmTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive || user.TwoFactorPendingSecretEncrypted is null)
        {
            return Result<ConfirmTwoFactorResult>.Failure(InvalidError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var rawSecret = _protector.Unprotect(user.TwoFactorPendingSecretEncrypted);

        if (!_totpService.ValidateCode(rawSecret, command.Code, utcNow))
        {
            return Result<ConfirmTwoFactorResult>.Failure(InvalidError, ErrorType.Validation);
        }

        user.ConfirmTwoFactorEnrollment(utcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var recoveryCodes = await _recoveryCodeService.IssueNewBatchAsync(user.Id, utcNow, cancellationToken);

        return Result<ConfirmTwoFactorResult>.Success(new ConfirmTwoFactorResult(recoveryCodes));
    }
}
