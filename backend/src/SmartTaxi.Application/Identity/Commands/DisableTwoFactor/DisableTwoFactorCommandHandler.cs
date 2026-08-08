using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;

namespace SmartTaxi.Application.Identity.Commands.DisableTwoFactor;

public sealed class DisableTwoFactorCommandHandler : ICommandHandler<DisableTwoFactorCommand, Result>
{
    private const string UnauthorizedError = "Réauthentification requise.";
    private const string NotEnabledError = "La double authentification n'est pas activée.";
    private const string InvalidCodeError = "Le code est invalide.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorSecretProtector _protector;
    private readonly ITwoFactorRecoveryCodeRepository _recoveryCodeRepository;
    private readonly RecoveryCodeService _recoveryCodeService;

    public DisableTwoFactorCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITotpService totpService,
        ITwoFactorSecretProtector protector,
        ITwoFactorRecoveryCodeRepository recoveryCodeRepository,
        RecoveryCodeService recoveryCodeService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _protector = protector;
        _recoveryCodeRepository = recoveryCodeRepository;
        _recoveryCodeService = recoveryCodeService;
    }

    public async Task<Result> Handle(DisableTwoFactorCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        // Strong reauthentication: current password AND a valid code (TOTP or
        // recovery code) are both required — neither alone is sufficient.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash.Value, command.CurrentPassword))
        {
            return Result.Failure(UnauthorizedError, ErrorType.Unauthorized);
        }

        if (!user.TwoFactorEnabled || user.TwoFactorActiveSecretEncrypted is null)
        {
            return Result.Failure(NotEnabledError, ErrorType.Validation);
        }

        var utcNow = DateTime.UtcNow;
        var rawSecret = _protector.Unprotect(user.TwoFactorActiveSecretEncrypted);
        var codeValid = _totpService.ValidateCode(rawSecret, command.Code, utcNow)
            || await _recoveryCodeService.TryConsumeMatchingAsync(user.Id, command.Code, utcNow, cancellationToken);

        if (!codeValid)
        {
            return Result.Failure(InvalidCodeError, ErrorType.Unauthorized);
        }

        user.DisableTwoFactor();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _recoveryCodeRepository.DeleteAllForUserAsync(user.Id, cancellationToken);

        return Result.Success();
    }
}
