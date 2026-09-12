using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Notifications.Commands.DeactivateNotificationTemplate;

public sealed record DeactivateNotificationTemplateCommand(Guid TemplateId) : ICommand<Result>;
