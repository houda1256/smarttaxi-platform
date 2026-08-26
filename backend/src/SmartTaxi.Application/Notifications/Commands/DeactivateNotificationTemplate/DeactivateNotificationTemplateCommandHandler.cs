using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.DeactivateNotificationTemplate;

public sealed class DeactivateNotificationTemplateCommandHandler : ICommandHandler<DeactivateNotificationTemplateCommand, Result>
{
    private const string NotFoundError = "Modèle de notification introuvable.";

    private readonly INotificationTemplateRepository _templateRepository;

    public DeactivateNotificationTemplateCommandHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Result> Handle(DeactivateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        template.Deactivate(DateTime.UtcNow);
        await _templateRepository.UpdateAsync(template, cancellationToken);

        return Result.Success();
    }
}
