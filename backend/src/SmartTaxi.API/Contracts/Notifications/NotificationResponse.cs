using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.API.Contracts.Notifications;

public sealed record NotificationResponse(
    Guid Id, string Category, string TemplateKey, string Title, string Body, bool IsMandatory, bool IsRead,
    DateTime CreatedAtUtc, DateTime? ReadAtUtc, string SourceType, Guid SourceId)
{
    public static NotificationResponse FromEntity(Notification notification) => new(
        notification.Id, notification.Category.ToString(), notification.TemplateKey, notification.Title, notification.Body,
        notification.IsMandatory, notification.IsRead, notification.CreatedAtUtc, notification.ReadAtUtc,
        notification.SourceType, notification.SourceId);
}
