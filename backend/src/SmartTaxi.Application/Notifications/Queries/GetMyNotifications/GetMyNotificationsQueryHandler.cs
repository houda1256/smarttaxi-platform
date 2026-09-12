using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler : IQueryHandler<GetMyNotificationsQuery, PagedResult<Notification>>
{
    private const int MaxPageSize = 100;

    private readonly INotificationRepository _notificationRepository;

    public GetMyNotificationsQueryHandler(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public Task<PagedResult<Notification>> Handle(GetMyNotificationsQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? MaxPageSize : query.PageSize;

        return _notificationRepository.GetForRecipientAsync(query.UserId, query.UnreadOnly, pageNumber, pageSize, cancellationToken);
    }
}
