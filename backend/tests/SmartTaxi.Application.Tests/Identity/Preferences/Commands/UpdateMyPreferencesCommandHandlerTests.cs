using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Preferences.Commands.UpdateMyPreferences;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;

namespace SmartTaxi.Application.Tests.Identity.Preferences.Commands;

public class UpdateMyPreferencesCommandHandlerTests
{
    private readonly FakeUserPreferencesRepository _repository = new();
    private readonly UpdateMyPreferencesCommandHandler _handler;

    public UpdateMyPreferencesCommandHandlerTests()
    {
        _handler = new UpdateMyPreferencesCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ForFirstTimeUpdate_CreatesPreferencesWithGivenValues()
    {
        var userId = Guid.NewGuid();

        var result = await _handler.Handle(
            new UpdateMyPreferencesCommand(
                userId, Language.Arabic, NotificationChannel.Sms, true, true, "UTC", "Nom", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Language.Arabic, result.Value!.Language);
        Assert.Equal(NotificationChannel.Sms, result.Value.NotificationChannels);
        Assert.True(result.Value.ShareProfileWithPartners);
    }

    [Fact]
    public async Task Handle_ForSubsequentUpdate_ReplacesPreviousValues()
    {
        var userId = Guid.NewGuid();
        await _handler.Handle(
            new UpdateMyPreferencesCommand(userId, Language.French, NotificationChannel.Email, false, false, "UTC", null, null),
            CancellationToken.None);

        var result = await _handler.Handle(
            new UpdateMyPreferencesCommand(
                userId, Language.English, NotificationChannel.Push | NotificationChannel.InApp, true, false, "UTC", "New Name", null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(Language.English, result.Value!.Language);
        Assert.Equal(NotificationChannel.Push | NotificationChannel.InApp, result.Value.NotificationChannels);
        Assert.Equal("New Name", result.Value.DisplayName);
    }

    [Fact]
    public async Task Handle_WithInvalidTimezone_ReturnsValidationError()
    {
        var result = await _handler.Handle(
            new UpdateMyPreferencesCommand(
                Guid.NewGuid(), Language.French, NotificationChannel.Email, false, false, "Not/A/Real/Zone", null, null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }
}
