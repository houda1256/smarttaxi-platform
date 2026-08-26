using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Commands.CreateNotificationTemplate;

public sealed class CreateNotificationTemplateCommandHandler : ICommandHandler<CreateNotificationTemplateCommand, Result<Guid>>
{
    private const string DuplicateError = "Un modèle existe déjà pour cette clé, ce canal et cette langue.";

    private readonly INotificationTemplateRepository _templateRepository;

    public CreateNotificationTemplateCommandHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public async Task<Result<Guid>> Handle(CreateNotificationTemplateCommand command, CancellationToken cancellationToken)
    {
        var existing = await _templateRepository.GetAsync(command.TemplateKey, command.Channel, command.Language, cancellationToken);

        if (existing is not null)
        {
            return Result<Guid>.Failure(DuplicateError, ErrorType.Conflict);
        }

        NotificationTemplate template;

        try
        {
            template = NotificationTemplate.Create(
                command.TemplateKey, command.Category, command.Channel, command.Language, command.Subject, command.Body, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        await _templateRepository.AddAsync(template, cancellationToken);

        return Result<Guid>.Success(template.Id);
    }
}
