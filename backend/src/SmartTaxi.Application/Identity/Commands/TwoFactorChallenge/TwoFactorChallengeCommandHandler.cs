using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;

namespace SmartTaxi.Application.Identity.Commands.TwoFactorChallenge;

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

    public TwoFactorChallengeCommandHandler(
        ITwoFactorChallengeRepository challengeRepository,
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IRefreshTokenHasher hasher,
        ITotpService totpService,
        ITwoFactorSecretProtector protector,
        RecoveryCodeService recoveryCodeService,
        RefreshTokenIssuer refreshTokenIssuer)
    {
        _challengeRepository = challengeRepository;
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _hasher = hasher;
        _totpService = totpService;
        _protector = protector;
        _recoveryCodeService = recoveryCodeService;
        _refreshTokenIssuer = refreshTokenIssuer;
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

        var rawSecret = _protector.Unprotect(user.TwoFactorActiveSecretEncrypted);
        var codeValid = _totpService.ValidateCode(rawSecret, command.Code, utcNow)
            || await _recoveryCodeService.TryConsumeMatchingAsync(user.Id, command.Code, utcNow, cancellationToken);

        if (!codeValid)
        {
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

        return Result<TwoFactorChallengeResult>.Success(new TwoFactorChallengeResult(accessToken, rawRefreshToken));
    }
}
