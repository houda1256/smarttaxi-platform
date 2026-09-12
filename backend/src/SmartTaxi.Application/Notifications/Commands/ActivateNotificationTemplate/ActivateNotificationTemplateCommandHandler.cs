using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.ActivateNotificationTemplate;

public sealed class ActivateNotificationTemplateCommandHandler : ICommandHandler<ActivateNotificationTemplateCommand, Result>
{
    private const string NotFoundError = "Modèle de notification introuvable.";

    private readonly INotificationTemplateRepository _templateRepository;

    public ActivateNotificationTemplateCommandHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Result> Handle(ActivateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        template.Activate(DateTime.UtcNow);
        await _templateRepository.UpdateAsync(template, cancellationToken);

        return Result.Success();
    }
}
