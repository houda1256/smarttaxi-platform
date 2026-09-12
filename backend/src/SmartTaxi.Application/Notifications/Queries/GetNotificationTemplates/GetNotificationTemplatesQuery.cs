using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetNotificationTemplates;

public sealed record GetNotificationTemplatesQuery : IQuery<IReadOnlyCollection<NotificationTemplate>>;
