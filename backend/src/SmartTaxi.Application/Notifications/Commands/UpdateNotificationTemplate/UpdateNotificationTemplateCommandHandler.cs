using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;

namespace SmartTaxi.Application.Notifications.Commands.UpdateNotificationTemplate;

public sealed class UpdateNotificationTemplateCommandHandler : ICommandHandler<UpdateNotificationTemplateCommand, Result>
{
    private const string NotFoundError = "Modèle de notification introuvable.";

    private readonly INotificationTemplateRepository _templateRepository;

    public UpdateNotificationTemplateCommandHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Result> Handle(UpdateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(command.TemplateId, cancellationToken);

        if (template is null)
        {
            return Result.Failure(NotFoundError, ErrorType.NotFound);
        }

        try
        {
            template.UpdateContent(command.Subject, command.Body, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(ex.Message, ErrorType.Validation);
        }

        await _templateRepository.UpdateAsync(template, cancellationToken);

        return Result.Success();
    }
}
