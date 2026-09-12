using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Queries.GetMyUnreadNotificationCount;

public sealed class GetMyUnreadNotificationCountQueryHandler : IQueryHandler<GetMyUnreadNotificationCountQuery, int>
{
    private readonly INotificationRepository _notificationRepository;

    public GetMyUnreadNotificationCountQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task<int> Handle(GetMyUnreadNotificationCountQuery query, CancellationToken cancellationToken) =>
        _notificationRepository.GetUnreadCountAsync(query.UserId, cancellationToken);
}
