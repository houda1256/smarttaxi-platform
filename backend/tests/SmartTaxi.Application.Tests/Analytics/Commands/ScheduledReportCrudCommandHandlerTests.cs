using SmartTaxi.Application.Analytics.Commands.CreateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.DeactivateScheduledReport;
using SmartTaxi.Application.Analytics.Commands.UpdateScheduledReport;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Analytics.Enums;

namespace SmartTaxi.Application.Tests.Analytics.Commands;

public class ScheduledReportCrudCommandHandlerTests
{
    [Fact]
    public async Task Create_ValidRequest_Succeeds()
    {
        var repository = new FakeScheduledReportRepository();
        var handler = new CreateScheduledReportCommandHandler(repository);

        var result = await handler.Handle(
            new CreateScheduledReportCommand(
                ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await repository.GetByIdAsync(result.Value, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.True(stored!.IsActive);
    }

    [Fact]
    public async Task Create_EmptyRecipient_ReturnsValidation()
    {
        var handler = new CreateScheduledReportCommandHandler(new FakeScheduledReportRepository());

        var result = await handler.Handle(
            new CreateScheduledReportCommand(
                ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.Empty, DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Update_ExistingDefinition_Succeeds()
    {
        var repository = new FakeScheduledReportRepository();
        var createHandler = new CreateScheduledReportCommandHandler(repository);
        var createResult = await createHandler.Handle(
            new CreateScheduledReportCommand(
                ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        var newRecipient = Guid.NewGuid();
        var updateHandler = new UpdateScheduledReportCommandHandler(repository);
        var updateResult = await updateHandler.Handle(
            new UpdateScheduledReportCommand(
                createResult.Value, ScheduledReportCategory.Support, ScheduledReportFrequency.Weekly, newRecipient),
            CancellationToken.None);

        Assert.True(updateResult.IsSuccess);
        var stored = await repository.GetByIdAsync(createResult.Value, CancellationToken.None);
        Assert.Equal(ScheduledReportCategory.Support, stored!.Category);
        Assert.Equal(ScheduledReportFrequency.Weekly, stored.Frequency);
        Assert.Equal(newRecipient, stored.RecipientUserId);
    }

    [Fact]
    public async Task Update_UnknownDefinition_ReturnsNotFound()
    {
        var handler = new UpdateScheduledReportCommandHandler(new FakeScheduledReportRepository());

        var result = await handler.Handle(
            new UpdateScheduledReportCommand(
                Guid.NewGuid(), ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task Deactivate_ExistingDefinition_SetsIsActiveFalse()
    {
        var repository = new FakeScheduledReportRepository();
        var createHandler = new CreateScheduledReportCommandHandler(repository);
        var createResult = await createHandler.Handle(
            new CreateScheduledReportCommand(
                ScheduledReportCategory.Ride, ScheduledReportFrequency.Daily, Guid.NewGuid(), DateTime.UtcNow.AddDays(1)),
            CancellationToken.None);

        var handler = new DeactivateScheduledReportCommandHandler(repository);
        var result = await handler.Handle(new DeactivateScheduledReportCommand(createResult.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var stored = await repository.GetByIdAsync(createResult.Value, CancellationToken.None);
        Assert.False(stored!.IsActive);
    }
}
