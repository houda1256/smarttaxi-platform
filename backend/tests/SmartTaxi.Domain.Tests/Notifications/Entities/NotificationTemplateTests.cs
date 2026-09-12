using SmartTaxi.Domain.Identity.Preferences.Enums;
using SmartTaxi.Domain.Notifications.Entities;
using SmartTaxi.Domain.Notifications.Enums;

namespace SmartTaxi.Domain.Tests.Notifications.Entities;

public class NotificationTemplateTests
{
    private static NotificationTemplate NewTemplate() => NotificationTemplate.Create(
        "subscription.renewed", NotificationCategory.Subscription, NotificationChannel.Email, Language.French,
        "Subject {PlanName}", "Body {PlanName}", DateTime.UtcNow);

    [Fact]
    public void Create_SetsActiveAndVersionOne()
    {
        var template = NewTemplate();

        Assert.True(template.IsActive);
        Assert.Equal(1, template.Version);
    }

    [Fact]
    public void Create_WithBlankBody_Throws()
    {
        Assert.Throws<ArgumentException>(() => NotificationTemplate.Create(
            "key", NotificationCategory.System, NotificationChannel.Email, Language.French, "subject", " ", DateTime.UtcNow));
    }

    [Fact]
    public void UpdateContent_IncrementsVersion()
    {
        var template = NewTemplate();

        template.UpdateContent("New subject", "New body", DateTime.UtcNow);

        Assert.Equal(2, template.Version);
        Assert.Equal("New body", template.Body);
    }

    [Fact]
    public void Deactivate_ThenActivate_TogglesIsActive()
    {
        var template = NewTemplate();

        template.Deactivate(DateTime.UtcNow);
        Assert.False(template.IsActive);

        template.Activate(DateTime.UtcNow);
        Assert.True(template.IsActive);
    }
}
