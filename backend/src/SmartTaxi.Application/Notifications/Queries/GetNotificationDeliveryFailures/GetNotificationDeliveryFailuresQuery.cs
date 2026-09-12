using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetNotificationDeliveryFailures;

public sealed record GetNotificationDeliveryFailuresQuery(int PageNumber, int PageSize) : IQuery<PagedResult<NotificationDeliveryAttempt>>;
