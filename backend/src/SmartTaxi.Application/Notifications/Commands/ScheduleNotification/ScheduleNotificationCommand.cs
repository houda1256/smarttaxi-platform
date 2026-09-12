using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Notifications.Commands.ScheduleNotification;

/// <summary>The explicit hook other modules (or a future scheduler) use to plan a future reminder — e.g. "subscription expiring in 3 days". See ProcessDueNotificationsCommand for the sweep that turns these into real dispatches once due.</summary>
public sealed record ScheduleNotificationCommand(
    Guid RecipientUserId, NotificationCategory Category, string TemplateKey, IReadOnlyDictionary<string, string> Variables,
    bool IsMandatory, string SourceType, Guid SourceId, DateTime ScheduledAtUtc) : ICommand<Result<Guid>>;
