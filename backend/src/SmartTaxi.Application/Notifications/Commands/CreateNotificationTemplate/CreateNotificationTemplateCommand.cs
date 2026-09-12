using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed record CreateNotificationTemplateCommand(
    string TemplateKey, NotificationCategory Category, NotificationChannel Channel, Language Language, string? Subject, string Body)
    : ICommand<Result<Guid>>;
