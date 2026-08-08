using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;

namespace SmartTaxi.Application.Identity.Commands.ChangePassword;

public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, Result>
{
    private const int MinPasswordLength = 8;
    private const string InvalidCurrentPasswordError = "Le mot de passe actuel est incorrect.";

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISecurityPolicy _securityPolicy;

    public ChangePasswordCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ISessionRepository sessionRepository,
        ISecurityPolicy securityPolicy)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _sessionRepository = sessionRepository;
        _securityPolicy = securityPolicy;
    }

    public async Task<Result> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(user.PasswordHash.Value, command.CurrentPassword))
        {
            return Result.Failure(InvalidCurrentPasswordError, ErrorType.Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < MinPasswordLength)
        {
            return Result.Failure(
                $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères.", ErrorType.Validation);
        }

        user.ChangePassword(HashedPassword.Create(_passwordHasher.Hash(command.NewPassword)));
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Policy: the current session (the one that just proved continued
        // control via the current password) always stays active; every other
        // session is revoked by default, configurable via SecurityOptions.
        if (_securityPolicy.RevokeOtherSessionsOnPasswordChange)
        {
            await _sessionRepository.RevokeAllActiveSessionsAsync(
                user.Id, SessionRevocationReason.PasswordChanged, DateTime.UtcNow, cancellationToken, command.CurrentSessionId);
        }

        return Result.Success();
    }
}
