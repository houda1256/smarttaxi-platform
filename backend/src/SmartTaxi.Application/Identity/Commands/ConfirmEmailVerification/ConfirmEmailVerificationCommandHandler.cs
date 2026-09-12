using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Identity.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandHandler : ICommandHandler<ConfirmEmailVerificationCommand, Result>
{
    private const string InvalidTokenError = "Le lien de vérification est invalide ou a expiré.";

    private readonly IEmailVerificationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenHasher _tokenHasher;

    public ConfirmEmailVerificationCommandHandler(
        IEmailVerificationTokenRepository tokenRepository,
        IUserRepository userRepository,
        IRefreshTokenHasher tokenHasher)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _tokenHasher = tokenHasher;
    }

    public async Task<Result> Handle(ConfirmEmailVerificationCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenHasher.Hash(command.RawToken);
        var token = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        var utcNow = DateTime.UtcNow;

        if (token is null || !token.IsValid(utcNow))
        {
            return Result.Failure(InvalidTokenError, ErrorType.Validation);
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure(InvalidTokenError, ErrorType.Validation);
        }

        var consumed = await _tokenRepository.TryConsumeAsync(token.Id, utcNow, cancellationToken);

        if (!consumed)
        {
            return Result.Failure(InvalidTokenError, ErrorType.Validation);
        }

        user.VerifyEmail(utcNow);
        await _userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success();
    }
}
