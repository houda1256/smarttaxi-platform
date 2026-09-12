using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Commands.ScheduleNotification;

public sealed class ScheduleNotificationCommandHandler : ICommandHandler<ScheduleNotificationCommand, Result<Guid>>
{
    private const string DuplicateError = "Un rappel équivalent est déjà planifié pour ce destinataire.";

    private readonly IScheduledNotificationRepository _scheduledNotificationRepository;

    public ScheduleNotificationCommandHandler(IScheduledNotificationRepository scheduledNotificationRepository)
    {
        _scheduledNotificationRepository = scheduledNotificationRepository;
    }

    public async Task<Result<Guid>> Handle(ScheduleNotificationCommand command, CancellationToken cancellationToken)
    {
        ScheduledNotification scheduled;

        try
        {
            scheduled = ScheduledNotification.Schedule(
                command.RecipientUserId, command.Category, command.TemplateKey, command.Variables, command.IsMandatory,
                command.SourceType, command.SourceId, command.ScheduledAtUtc, DateTime.UtcNow);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message, ErrorType.Validation);
        }

        var added = await _scheduledNotificationRepository.TryAddAsync(scheduled, cancellationToken);

        return added ? Result<Guid>.Success(scheduled.Id) : Result<Guid>.Failure(DuplicateError, ErrorType.Conflict);
    }
}
