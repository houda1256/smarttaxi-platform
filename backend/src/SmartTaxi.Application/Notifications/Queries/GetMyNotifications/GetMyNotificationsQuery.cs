using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetMyNotifications;

public sealed record GetMyNotificationsQuery(Guid UserId, bool UnreadOnly, int PageNumber, int PageSize) : IQuery<PagedResult<Notification>>;
