using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllNotificationsReadCommandHandler : ICommandHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    private readonly INotificationRepository _notificationRepository;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<Result<int>> Handle(MarkAllNotificationsReadCommand command, CancellationToken cancellationToken)
    {
        var count = await _notificationRepository.MarkAllAsReadAsync(command.RequestingUserId, DateTime.UtcNow, cancellationToken);
        return Result<int>.Success(count);
    }
}
