namespace SmartTaxi.API.Contracts.Notifications;

public sealed record CreateNotificationTemplateRequest(
    string TemplateKey, string Category, string Channel, string Language, string? Subject, string Body);

public sealed record UpdateNotificationTemplateRequest(string? Subject, string Body);
