using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.MarkNotificationRead;

/// <summary>Ownership is checked here (Forbidden on mismatch) and re-checked atomically in the repository call — defense in depth, same convention as the rest of the codebase's owner-scoped mutations.</summary>
public sealed class MarkNotificationReadCommandHandler : ICommandHandler<MarkNotificationReadCommand, Result>
{
    private const string NotFoundError = "Notification introuvable.";
    private const string NotOwnerError = "Seul le destinataire peut marquer cette notification comme lue.";

    private readonly INotificationRepository _notificationRepository;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand command, CancellationToken cancellationToken)
    {
        var notification = await _notificationRepository.GetByIdAsync(command.NotificationId, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (notification.RecipientUserId != command.RequestingUserId)
        {
            return Result.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        await _notificationRepository.TryMarkAsReadAsync(command.NotificationId, command.RequestingUserId, DateTime.UtcNow, cancellationToken);

        return Result.Success();
    }
}
