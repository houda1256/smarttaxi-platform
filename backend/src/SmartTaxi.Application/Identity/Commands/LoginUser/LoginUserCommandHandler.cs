using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using TwoFactorChallengeEntity = SmartTaxi.Domain.Identity.Entities.TwoFactorChallenge;

namespace SmartTaxi.Application.Identity.Commands.LoginUser;

/// <summary>
/// Lockout is checked in the SAME short-circuit chain IsActive already used
/// (before password verification), extending an already-accepted pattern
/// rather than introducing a new timing side-channel class — a deactivated
/// or locked-out account already responded faster than a wrong-password
/// attempt on an active account before this change, since the password
/// hasher is never invoked for either. Every failure branch returns the
/// exact same generic message: account existence, suspension, and lockout
/// state are never distinguishable from the response.
/// </summary>
public sealed class LoginUserCommandHandler
    : ICommandHandler<LoginUserCommand, Result<LoginUserResult>>
{
    private const string InvalidCredentialsError = "Email ou mot de passe incorrect.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionRepository _sessionRepository;
    private readonly RefreshTokenIssuer _refreshTokenIssuer;
    private readonly ITwoFactorChallengeRepository _challengeRepository;
    private readonly IRefreshTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly ITwoFactorPolicy _twoFactorPolicy;
    private readonly ILoginLockoutPolicy _lockoutPolicy;
    private readonly IAuditLogRepository _auditLogRepository;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionRepository sessionRepository,
        RefreshTokenIssuer refreshTokenIssuer,
        ITwoFactorChallengeRepository challengeRepository,
        IRefreshTokenGenerator tokenGenerator,
        IRefreshTokenHasher tokenHasher,
        ITwoFactorPolicy twoFactorPolicy,
        ILoginLockoutPolicy lockoutPolicy,
        IAuditLogRepository auditLogRepository)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionRepository = sessionRepository;
        _refreshTokenIssuer = refreshTokenIssuer;
        _challengeRepository = challengeRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _twoFactorPolicy = twoFactorPolicy;
        _lockoutPolicy = lockoutPolicy;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        if (!Email.TryCreate(command.Email, out var email, out _))
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Nonexistent and deactivated accounts never reach the lockout check or
        // the password hasher at all — recording a failed attempt for either
        // would be impossible (no row to increment) or semantically incorrect
        // (a deactivated account isn't "trying" anything).
        if (user is null || !user.IsActive)
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        var utcNow = DateTime.UtcNow;

        if (user.IsLockedOut(utcNow))
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        if (!_passwordHasher.Verify(user.PasswordHash.Value, command.Password))
        {
            await RecordFailedAttemptAsync(user.Id, utcNow, cancellationToken);
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        if (user.TwoFactorEnabled)
        {
            // No session is created yet — it's only born once the challenge is
            // answered correctly. The original login's device label is carried
            // through the challenge so it still reaches the eventual session.
            // Deliberately does NOT reset FailedLoginAttempts here — password
            // success alone is not a complete authentication while 2FA is
            // still pending; only TwoFactorChallengeCommandHandler's own
            // success path resets it.
            var rawChallengeToken = _tokenGenerator.Generate();
            var challengeHash = _tokenHasher.Hash(rawChallengeToken);
            var challenge = new TwoFactorChallengeEntity(
                user.Id, challengeHash, command.DeviceLabel, utcNow.Add(_twoFactorPolicy.ChallengeTokenLifetime), utcNow);
            await _challengeRepository.AddAsync(challenge, cancellationToken);

            return Result<LoginUserResult>.Success(LoginUserResult.TwoFactorRequired(rawChallengeToken));
        }

        var (session, rawRefreshToken, accessToken) =
            await _refreshTokenIssuer.StartSessionAsync(user, command.DeviceLabel, cancellationToken);

        await _sessionRepository.AddAsync(session, cancellationToken);
        await _userRepository.ResetFailedLoginAttemptsAsync(user.Id, cancellationToken);

        return Result<LoginUserResult>.Success(LoginUserResult.Authenticated(accessToken, rawRefreshToken));
    }

    /// <summary>
    /// Increments the shared authentication-failure counter and, only on the
    /// exact transition into a locked state (newCount == MaxFailedAttempts,
    /// never on every subsequent attempt while already locked — those never
    /// reach this method at all, since IsLockedOut short-circuits first),
    /// writes a best-effort AccountLocked audit entry (system-triggered, no
    /// human actor, not required to be transactionally bound to the counter
    /// increment itself per the approved design).
    /// </summary>
    private async Task RecordFailedAttemptAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var newCount = await _userRepository.RecordFailedLoginAttemptAsync(
            userId, _lockoutPolicy.MaxFailedAttempts, utcNow, _lockoutPolicy.LockoutDuration, cancellationToken);

        if (newCount == _lockoutPolicy.MaxFailedAttempts)
        {
            await _auditLogRepository.AddAsync(
                AuditLogEntry.Create(null, AuditAction.AccountLocked, AuditTargetType.User, userId, null, null, null, null, utcNow),
                cancellationToken);
        }
    }
}
