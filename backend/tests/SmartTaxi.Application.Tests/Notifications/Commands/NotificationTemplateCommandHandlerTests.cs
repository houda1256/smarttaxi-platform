using SmartTaxi.Application.Common;
using SmartTaxi.Application.Notifications.Commands.ActivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.CreateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.DeactivateNotificationTemplate;
using SmartTaxi.Application.Notifications.Commands.UpdateNotificationTemplate;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.Notifications.Commands;

public class NotificationTemplateCommandHandlerTests
{
    private readonly FakeNotificationTemplateRepository _templateRepository = new();
    private readonly CreateNotificationTemplateCommandHandler _createHandler;
    private readonly UpdateNotificationTemplateCommandHandler _updateHandler;
    private readonly ActivateNotificationTemplateCommandHandler _activateHandler;
    private readonly DeactivateNotificationTemplateCommandHandler _deactivateHandler;

    public NotificationTemplateCommandHandlerTests()
    {
        _createHandler = new CreateNotificationTemplateCommandHandler(_templateRepository);
        _updateHandler = new UpdateNotificationTemplateCommandHandler(_templateRepository);
        _activateHandler = new ActivateNotificationTemplateCommandHandler(_templateRepository);
        _deactivateHandler = new DeactivateNotificationTemplateCommandHandler(_templateRepository);
    }

    [Fact]
    public async Task Create_NewCombination_Succeeds()
    {
        var result = await _createHandler.Handle(
            new CreateNotificationTemplateCommand(
                "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French,
                "Sujet", "Corps"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Create_DuplicateKeyChannelLanguage_ReturnsConflict()
    {
        var command = new CreateNotificationTemplateCommand(
            "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French, "Sujet", "Corps");
        await _createHandler.Handle(command, CancellationToken.None);

        var result = await _createHandler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task Update_ExistingTemplate_IncrementsVersion()
    {
        var created = await _createHandler.Handle(
            new CreateNotificationTemplateCommand(
                "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French, "Sujet", "Corps"),
            CancellationToken.None);

        var result = await _updateHandler.Handle(
            new UpdateNotificationTemplateCommand(created.Value, "Nouveau sujet", "Nouveau corps"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var template = await _templateRepository.GetByIdAsync(created.Value, CancellationToken.None);
        Assert.Equal(2, template!.Version);
    }

    [Fact]
    public async Task Deactivate_ThenActivate_TogglesIsActive()
    {
        var created = await _createHandler.Handle(
            new CreateNotificationTemplateCommand(
                "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French, "Sujet", "Corps"),
            CancellationToken.None);

        await _deactivateHandler.Handle(new DeactivateNotificationTemplateCommand(created.Value), CancellationToken.None);
        var afterDeactivate = await _templateRepository.GetByIdAsync(created.Value, CancellationToken.None);
        Assert.False(afterDeactivate!.IsActive);

        await _activateHandler.Handle(new ActivateNotificationTemplateCommand(created.Value), CancellationToken.None);
        var afterActivate = await _templateRepository.GetByIdAsync(created.Value, CancellationToken.None);
        Assert.True(afterActivate!.IsActive);
    }

    [Fact]
    public async Task Update_UnknownTemplate_ReturnsNotFound()
    {
        var result = await _updateHandler.Handle(
            new UpdateNotificationTemplateCommand(Guid.NewGuid(), "Sujet", "Corps"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
