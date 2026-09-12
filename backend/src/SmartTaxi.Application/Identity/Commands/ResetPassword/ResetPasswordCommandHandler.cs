using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.ResetPassword;

public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, Result>
{
    private const int MinPasswordLength = 8;
    private const string InvalidTokenError = "Le lien de réinitialisation est invalide ou a expiré.";

    private readonly IPasswordResetTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IRefreshTokenHasher _tokenHasher;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokenRepository,
        IUserRepository userRepository,
        ISessionRepository sessionRepository,
        IRefreshTokenHasher tokenHasher,
        IPasswordHasher passwordHasher)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _tokenHasher = tokenHasher;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < MinPasswordLength)
        {
            return Result.Failure(
                $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères.", ErrorType.Validation);
        }

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

        user.ChangePassword(HashedPassword.Create(_passwordHasher.Hash(command.NewPassword)));
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Unconditional, no exception — password reset always revokes every
        // active session, since there is no "current session" to preserve in
        // this unauthenticated flow.
        await _sessionRepository.RevokeAllActiveSessionsAsync(
            user.Id, SessionRevocationReason.PasswordReset, utcNow, cancellationToken);

        return Result.Success();
    }
}
