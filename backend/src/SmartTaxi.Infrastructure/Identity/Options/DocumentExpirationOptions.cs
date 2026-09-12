namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class DocumentExpirationOptions
{
    public const string SectionName = "DocumentExpiration";

    public int ReminderLeadDays { get; init; } = 30;
}
