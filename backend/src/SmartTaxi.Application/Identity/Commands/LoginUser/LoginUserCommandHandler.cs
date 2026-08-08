using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Domain.Identity.ValueObjects;
using TwoFactorChallengeEntity = SmartTaxi.Domain.Identity.Entities.TwoFactorChallenge;

namespace SmartTaxi.Application.Identity.Commands.LoginUser;

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

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionRepository sessionRepository,
        RefreshTokenIssuer refreshTokenIssuer,
        ITwoFactorChallengeRepository challengeRepository,
        IRefreshTokenGenerator tokenGenerator,
        IRefreshTokenHasher tokenHasher,
        ITwoFactorPolicy twoFactorPolicy)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionRepository = sessionRepository;
        _refreshTokenIssuer = refreshTokenIssuer;
        _challengeRepository = challengeRepository;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _twoFactorPolicy = twoFactorPolicy;
    }

    public async Task<Result<LoginUserResult>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        if (!Email.TryCreate(command.Email, out var email, out _))
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // IsActive is checked alongside the password to keep the same generic
        // message regardless of reason — this must not reveal whether an
        // account exists, is deactivated, or the password was simply wrong.
        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash.Value, command.Password))
        {
            return Result<LoginUserResult>.Failure(InvalidCredentialsError, ErrorType.Unauthorized);
        }

        if (user.TwoFactorEnabled)
        {
            // No session is created yet — it's only born once the challenge is
            // answered correctly. The original login's device label is carried
            // through the challenge so it still reaches the eventual session.
            var utcNow = DateTime.UtcNow;
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

        return Result<LoginUserResult>.Success(LoginUserResult.Authenticated(accessToken, rawRefreshToken));
    }
}
