using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Administration.Commands.ResetUserTwoFactor;

/// <summary>
/// Disables 2FA and invalidates every recovery code atomically (see
/// IAdminUserManagementRepository.TryResetTwoFactorAsync), then notifies the
/// account owner — a 2FA reset the owner didn't request is a strong signal
/// worth surfacing to them regardless, mirroring how payment-confirmation
/// notifications are already treated as mandatory for security-relevant
/// events. Never includes any secret/code in the notification payload.
/// </summary>
public sealed class ResetUserTwoFactorCommandHandler : ICommandHandler<ResetUserTwoFactorCommand, Result>
{
    private const string SelfTargetError = "Un administrateur ne peut pas réinitialiser sa propre double authentification via cet endpoint.";
    private const string NotFoundError = "Utilisateur introuvable.";
    private const string AlreadyDisabledError = "La double authentification n'est pas activée pour cet utilisateur.";

    private readonly IUserRepository _userRepository;
    private readonly IAdminUserManagementRepository _repository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public ResetUserTwoFactorCommandHandler(
        IUserRepository userRepository, IAdminUserManagementRepository repository, INotificationDispatcher notificationDispatcher)
    {
        _userRepository = userRepository;
        _repository = repository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result> Handle(ResetUserTwoFactorCommand command, CancellationToken cancellationToken)
    {
        if (command.TargetUserId == command.ActingAdminUserId)
        {
            return Result.Failure(SelfTargetError, ErrorType.Forbidden);
        }

        var target = await _userRepository.GetByIdAsync(command.TargetUserId, cancellationToken);

        if (target is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        var utcNow = DateTime.UtcNow;
        var reset = await _repository.TryResetTwoFactorAsync(command.TargetUserId, command.ActingAdminUserId, utcNow, cancellationToken);

        if (!reset)
        {
            return Result.Failure(AlreadyDisabledError, ErrorType.Conflict);
        }

        await _notificationDispatcher.DispatchAsync(
            new NotificationRequest(
                command.TargetUserId, NotificationCategory.Security, "security.two-factor.admin-reset", new Dictionary<string, string>(),
                IsMandatory: true, SourceType: "User", SourceId: command.TargetUserId),
            cancellationToken);

        return Result.Success();
    }
}
