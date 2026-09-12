using SmartTaxi.Application.Maintenance.Commands.GenerateMaintenanceReminders;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Tests.Maintenance.Commands;

public class GenerateMaintenanceRemindersCommandHandlerTests
{
    private readonly FakeMaintenanceRecordRepository _recordRepository = new();
    private readonly FakeNotificationDispatcher _notificationDispatcher = new();
    private readonly GenerateMaintenanceRemindersCommandHandler _handler;

    public GenerateMaintenanceRemindersCommandHandlerTests()
    {
        _handler = new GenerateMaintenanceRemindersCommandHandler(_recordRepository, _notificationDispatcher);
    }

    private static MaintenanceRecord CreateRecord(Guid ownerId, DateOnly? nextRecommendedServiceDate) => MaintenanceRecord.Create(
        Guid.NewGuid(), Guid.NewGuid(), ownerId, Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), null, [], 100m, null,
        nextRecommendedServiceDate, null, DateTime.UtcNow);

    [Fact]
    public async Task Handle_OverdueRecord_DispatchesReminderToOwner()
    {
        var ownerId = Guid.NewGuid();
        _recordRepository.Add(CreateRecord(ownerId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1))));

        var result = await _handler.Handle(new GenerateMaintenanceRemindersCommand(), CancellationToken.None);

        Assert.Equal(1, result.Value);
        var dispatched = Assert.Single(_notificationDispatcher.DispatchedRequests);
        Assert.Equal(ownerId, dispatched.RecipientUserId);
        Assert.False(dispatched.IsMandatory);
    }

    [Fact]
    public async Task Handle_FutureRecord_DoesNotDispatch()
    {
        _recordRepository.Add(CreateRecord(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30))));

        var result = await _handler.Handle(new GenerateMaintenanceRemindersCommand(), CancellationToken.None);

        Assert.Equal(0, result.Value);
        Assert.Empty(_notificationDispatcher.DispatchedRequests);
    }

    [Fact]
    public async Task Handle_RecordWithNoNextServiceDate_IsIgnored()
    {
        _recordRepository.Add(CreateRecord(Guid.NewGuid(), null));

        var result = await _handler.Handle(new GenerateMaintenanceRemindersCommand(), CancellationToken.None);

        Assert.Equal(0, result.Value);
    }
}
