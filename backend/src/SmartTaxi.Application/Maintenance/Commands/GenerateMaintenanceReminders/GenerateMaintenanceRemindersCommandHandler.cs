using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Application.Notifications.Contracts;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Maintenance.Commands.GenerateMaintenanceReminders;

/// <summary>
/// Preventive maintenance MVP: strictly date-based (NextRecommendedServiceDate
/// on the vehicle's own MaintenanceRecord history) — no mileage-based trigger
/// (Vehicle.CurrentMileage has no trustworthy automatic update pipeline per
/// the Module 9 audit), no rule engine, no ML. Re-running this sweep is safe:
/// Notifications' own SourceType/SourceId idempotency (SourceType=
/// "MaintenanceRecord", SourceId=record.Id) means a record that already fired
/// its reminder never fires a duplicate one on a later sweep.
/// </summary>
public sealed class GenerateMaintenanceRemindersCommandHandler : ICommandHandler<GenerateMaintenanceRemindersCommand, Result<int>>
{
    private readonly IMaintenanceRecordRepository _recordRepository;
    private readonly INotificationDispatcher _notificationDispatcher;

    public GenerateMaintenanceRemindersCommandHandler(IMaintenanceRecordRepository recordRepository, INotificationDispatcher notificationDispatcher)
    {
        _recordRepository = recordRepository;
        _notificationDispatcher = notificationDispatcher;
    }

    public async Task<Result<int>> Handle(GenerateMaintenanceRemindersCommand command, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dueRecords = await _recordRepository.GetDueForReminderAsync(today, cancellationToken);

        foreach (var record in dueRecords)
        {
            await _notificationDispatcher.DispatchAsync(
                new NotificationRequest(
                    record.OwnerUserId, NotificationCategory.Maintenance, "maintenance.reminder-due", new Dictionary<string, string>(),
                    IsMandatory: false, SourceType: "MaintenanceRecord", SourceId: record.Id),
                cancellationToken);
        }

        return Result<int>.Success(dueRecords.Count);
    }
}
