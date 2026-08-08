namespace SmartTaxi.Domain.Identity.Preferences.Enums;

[Flags]
public enum NotificationChannel
{
    None = 0,
    Email = 1,
    Sms = 2,
    Push = 4,
    InApp = 8
}
