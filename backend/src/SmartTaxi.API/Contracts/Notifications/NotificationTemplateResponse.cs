using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.API.Contracts.Notifications;

public sealed record NotificationTemplateResponse(
    Guid Id, string TemplateKey, string Category, string Channel, string Language, string? Subject, string Body, bool IsActive, int Version)
{
    public static NotificationTemplateResponse FromEntity(NotificationTemplate template) => new(
        template.Id, template.TemplateKey, template.Category.ToString(), template.Channel.ToString(), template.Language.ToString(),
        template.Subject, template.Body, template.IsActive, template.Version);
}
