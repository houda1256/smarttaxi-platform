using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Identity.Commands.RegisterUser;

public sealed class RegisterUserCommandHandler
    : ICommandHandler<RegisterUserCommand, Result<RegisterUserResult>>
{
    private const int MinPasswordLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly INotificationDispatcher _notificationDispatcher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository, IPasswordHasher passwordHasher, INotificationDispatcher notificationDispatcher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<RegisterUserResult>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        if (!Email.TryCreate(command.Email, out var email, out var emailError))
        {
            return Result<RegisterUserResult>.Failure(emailError, ErrorType.Validation);
        }

        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            return Result<RegisterUserResult>.Failure("Un compte existe déjà avec cet email.", ErrorType.Conflict);
        }

        if (string.IsNullOrWhiteSpace(command.Password) || command.Password.Length < MinPasswordLength)
        {
            return Result<RegisterUserResult>.Failure(
                $"Le mot de passe doit contenir au moins {MinPasswordLength} caractères.", ErrorType.Validation);
        }

        var hashedPassword = HashedPassword.Create(_passwordHasher.Hash(command.Password));
        var user = User.Create(email, hashedPassword, UserRole.Customer);

        await _userRepository.AddAsync(user, cancellationToken);

        // Optional/non-security notification — account creation itself never blocks on this, and OTP/verification
        // flows (RequestEmailVerification, etc.) keep sending directly through IEmailSender/ISmsSender unchanged.
        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                user.Id, NotificationCategory.Identity, "identity.welcome", new Dictionary<string, string>(),
                IsMandatory: false, SourceType: "User", SourceId: user.Id),
            cancellationToken);

        return Result<RegisterUserResult>.Success(new RegisterUserResult(user.Id, user.Email.Value));
    }
}
