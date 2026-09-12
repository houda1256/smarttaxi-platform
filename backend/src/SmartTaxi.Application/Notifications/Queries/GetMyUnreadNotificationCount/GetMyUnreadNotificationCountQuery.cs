using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Queries.GetMyUnreadNotificationCount;

public sealed record GetMyUnreadNotificationCountQuery(Guid UserId) : IQuery<int>;
