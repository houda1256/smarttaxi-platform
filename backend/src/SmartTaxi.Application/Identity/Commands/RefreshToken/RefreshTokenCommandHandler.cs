using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Sessions;
using SmartTaxi.Domain.Identity.Enums;

namespace SmartTaxi.Application.Identity.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, Result<RefreshTokenResult>>
{
    private const string InvalidTokenError = "Le token de rafraîchissement est invalide ou a expiré.";
    private const string ConcurrentRefreshError = "Ce token vient d'être utilisé par une autre requête.";

    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenHasher _refreshTokenHasher;
    private readonly RefreshTokenIssuer _refreshTokenIssuer;

    public RefreshTokenCommandHandler(
        ISessionRepository sessionRepository,
        IUserRepository userRepository,
        IRefreshTokenHasher refreshTokenHasher,
        RefreshTokenIssuer refreshTokenIssuer)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _refreshTokenHasher = refreshTokenHasher;
        _refreshTokenIssuer = refreshTokenIssuer;
    }

    public async Task<Result<RefreshTokenResult>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var presentedTokenHash = _refreshTokenHasher.Hash(command.RawRefreshToken);
        var session = await _sessionRepository.GetByRefreshTokenHashAsync(presentedTokenHash, cancellationToken);

        if (session is null)
        {
            return Result<RefreshTokenResult>.Failure(InvalidTokenError, ErrorType.Unauthorized);
        }

        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<RefreshTokenResult>.Failure(InvalidTokenError, ErrorType.Unauthorized);
        }

        var (rotationResult, rawRefreshToken, accessToken) =
            await _refreshTokenIssuer.RotateAsync(user, session, presentedTokenHash, cancellationToken);

        var saved = await _sessionRepository.UpdateAsync(session, cancellationToken);

        if (!saved)
        {
            return Result<RefreshTokenResult>.Failure(ConcurrentRefreshError, ErrorType.Conflict);
        }

        if (rotationResult.Outcome != TokenRotationOutcome.Success)
        {
            return Result<RefreshTokenResult>.Failure(InvalidTokenError, ErrorType.Unauthorized);
        }

        return Result<RefreshTokenResult>.Success(new RefreshTokenResult(accessToken!, rawRefreshToken!));
    }
}
