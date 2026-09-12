using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed record UpdateNotificationTemplateCommand(Guid TemplateId, string? Subject, string Body) : ICommand<Result>;
