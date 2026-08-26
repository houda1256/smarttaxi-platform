using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.ActivateNotificationTemplate;

public sealed record ActivateNotificationTemplateCommand(Guid TemplateId) : ICommand<Result>;
