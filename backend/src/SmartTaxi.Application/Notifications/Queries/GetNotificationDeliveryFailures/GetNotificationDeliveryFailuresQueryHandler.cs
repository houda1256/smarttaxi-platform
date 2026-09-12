using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetNotificationDeliveryFailures;

public sealed class GetNotificationDeliveryFailuresQueryHandler
    : IQueryHandler<GetNotificationDeliveryFailuresQuery, PagedResult<NotificationDeliveryAttempt>>
{
    private const int MaxPageSize = 100;

    private readonly INotificationDeliveryAttemptRepository _attemptRepository;

    public GetNotificationDeliveryFailuresQueryHandler(INotificationDeliveryAttemptRepository attemptRepository)
    {
        _attemptRepository = attemptRepository;
    }

    public Task<PagedResult<NotificationDeliveryAttempt>> Handle(GetNotificationDeliveryFailuresQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? MaxPageSize : query.PageSize;

        return _attemptRepository.GetFailedAsync(pageNumber, pageSize, cancellationToken);
    }
}
