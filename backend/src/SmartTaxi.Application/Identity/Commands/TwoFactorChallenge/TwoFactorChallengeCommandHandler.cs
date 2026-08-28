using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

/// <summary>
/// A failed TOTP/recovery-code attempt participates in the SAME unified
/// authentication-failure counter as a wrong password — otherwise a correct
/// password would leave an unbounded, unrelated brute-force surface against
/// the 6-digit TOTP code. Success here is the "complete authentication flow"
/// point that resets the counter (LoginUserCommandHandler's own 2FA-required
/// branch deliberately never resets it).
/// </summary>
public sealed class TwoFactorChallengeCommandHandler
    : ICommandHandler<TwoFactorChallengeCommand, Result<TwoFactorChallengeResult>>
{
    private const string InvalidError = "Le défi de double authentification est invalide ou a expiré.";

    private readonly ITwoFactorChallengeRepository _challengeRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenHasher _hasher;
    private readonly ITotpService _totpService;
    private readonly ITwoFactorSecretProtector _protector;
    private readonly RecoveryCodeService _recoveryCodeService;
    private readonly RefreshTokenIssuer _refreshTokenIssuer;
    private readonly ILoginLockoutPolicy _lockoutPolicy;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IAuditContextAccessor _auditContextAccessor;

    public TwoFactorChallengeCommandHandler(
        ITwoFactorChallengeRepository challengeRepository,
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IRefreshTokenHasher hasher,
        ITotpService totpService,
        ITwoFactorSecretProtector protector,
        RecoveryCodeService recoveryCodeService,
        RefreshTokenIssuer refreshTokenIssuer,
        ILoginLockoutPolicy lockoutPolicy,
        IAuditLogRepository auditLogRepository,
        IAuditContextAccessor auditContextAccessor)
    {
        _challengeRepository = challengeRepository;
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _hasher = hasher;
        _totpService = totpService;
        _protector = protector;
        _recoveryCodeService = recoveryCodeService;
        _refreshTokenIssuer = refreshTokenIssuer;
        _lockoutPolicy = lockoutPolicy;
        _auditLogRepository = auditLogRepository;
        _auditContextAccessor = auditContextAccessor;
    }

    public async Task<Result<TwoFactorChallengeResult>> Handle(TwoFactorChallengeCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _hasher.Hash(command.ChallengeToken);
        var challenge = await _challengeRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (challenge is null || !challenge.IsValid(utcNow))
        {
            return Result<TwoFactorChallengeResult>.Failure(InvalidError, ErrorType.Unauthorized);
        }

        var user = await _userRepository.GetByIdAsync(challenge.UserId, cancellationToken);

        if (user is null || !user.IsActive || !user.TwoFactorEnabled || user.TwoFactorActiveSecretEncrypted is null)
        {
            return Result<TwoFactorChallengeResult>.Failure(InvalidError, ErrorType.Unauthorized);
        }

        if (user.IsLockedOut(utcNow))
        {
            return Result<TwoFactorChallengeResult>.Failure(InvalidError, ErrorType.Unauthorized);
        }

        var rawSecret = _protector.Unprotect(user.TwoFactorActiveSecretEncrypted);
        var codeValid = _totpService.ValidateCode(rawSecret, command.Code, utcNow)
            || await _recoveryCodeService.TryConsumeMatchingAsync(user.Id, command.Code, utcNow, cancellationToken);

        if (!codeValid)
        {
            var newCount = await _userRepository.RecordFailedLoginAttemptAsync(
                user.Id, _lockoutPolicy.MaxFailedAttempts, utcNow, _lockoutPolicy.LockoutDuration, cancellationToken);

            if (newCount == _lockoutPolicy.MaxFailedAttempts)
            {
                var context = _auditContextAccessor.GetContext();
                await _auditLogRepository.AddAsync(
                    AuditLogEntry.Create(
                        null, AuditAction.AccountLocked, AuditTargetType.User, user.Id, null,
                        context.CorrelationId, context.IpAddress, context.UserAgent, utcNow),
                    cancellationToken);
            }

            return Result<TwoFactorChallengeResult>.Failure(InvalidError, ErrorType.Unauthorized);
        }

        var consumed = await _challengeRepository.TryConsumeAsync(challenge.Id, utcNow, cancellationToken);

        if (!consumed)
        {
            return Result<TwoFactorChallengeResult>.Failure(InvalidError, ErrorType.Unauthorized);
        }

        var (session, rawRefreshToken, accessToken) =
            await _refreshTokenIssuer.StartSessionAsync(user, challenge.DeviceLabel, cancellationToken);
        await _sessionRepository.AddAsync(session, cancellationToken);
        await _userRepository.ResetFailedLoginAttemptsAsync(user.Id, cancellationToken);

        return Result<TwoFactorChallengeResult>.Success(new TwoFactorChallengeResult(accessToken, rawRefreshToken));
    }
}
