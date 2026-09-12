using SmartTaxi.Application.Notifications;
using SmartTaxi.Application.Tests.TestDoubles;
using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Application.Tests.Notifications;

public class NotificationTemplateRendererTests
{
    private readonly FakeNotificationTemplateRepository _templateRepository = new();
    private readonly NotificationTemplateRenderer _renderer;

    public NotificationTemplateRendererTests()
    {
        _renderer = new NotificationTemplateRenderer(_templateRepository);
    }

    [Fact]
    public async Task RenderAsync_WithMatchingLanguageTemplate_SubstitutesVariables()
    {
        var template = NotificationTemplate.Create(
            "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.English,
            "Renewed: {PlanName}", "Your plan {PlanName} was renewed.", DateTime.UtcNow);
        await _templateRepository.AddAsync(template, CancellationToken.None);

        var content = await _renderer.RenderAsync(
            "subscription.renewed", NotificationChannel.Email, Language.English,
            new Dictionary<string, string> { ["PlanName"] = "Driver Pro" }, CancellationToken.None);

        Assert.Equal("Renewed: Driver Pro", content.Subject);
        Assert.Equal("Your plan Driver Pro was renewed.", content.Body);
        Assert.False(content.UsedFallback);
    }

    [Fact]
    public async Task RenderAsync_WhenRequestedLanguageMissing_FallsBackToConfiguredDefaultLanguage()
    {
        var frenchTemplate = NotificationTemplate.Create(
            "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French,
            "Sujet", "Corps", DateTime.UtcNow);
        await _templateRepository.AddAsync(frenchTemplate, CancellationToken.None);

        // Arabic template was never authored — should transparently fall back to French (the configured default).
        var content = await _renderer.RenderAsync(
            "subscription.renewed", NotificationChannel.Email, Language.Arabic, new Dictionary<string, string>(), CancellationToken.None);

        Assert.Equal("Sujet", content.Subject);
        Assert.Equal("Corps", content.Body);
        Assert.False(content.UsedFallback);
    }

    [Fact]
    public async Task RenderAsync_WhenNoTemplateExistsInAnyLanguage_UsesDeterministicFallback()
    {
        var content = await _renderer.RenderAsync(
            "unknown.template.key", NotificationChannel.Email, Language.Arabic,
            new Dictionary<string, string> { ["Amount"] = "42.00" }, CancellationToken.None);

        Assert.True(content.UsedFallback);
        Assert.Null(content.Subject);
        Assert.Contains("unknown.template.key", content.Body);
        Assert.Contains("Amount: 42.00", content.Body);
    }

    [Fact]
    public async Task RenderAsync_WhenTemplateIsInactive_UsesDeterministicFallback()
    {
        var template = NotificationTemplate.Create(
            "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French,
            "Sujet", "Corps", DateTime.UtcNow);
        template.Deactivate(DateTime.UtcNow);
        await _templateRepository.AddAsync(template, CancellationToken.None);

        var content = await _renderer.RenderAsync(
            "subscription.renewed", NotificationChannel.Email, Language.French, new Dictionary<string, string>(), CancellationToken.None);

        Assert.True(content.UsedFallback);
    }
}
