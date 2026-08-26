namespace SmartTaxi.Infrastructure.Advertising.Options;

public sealed class AdvertisingOptions
{
    public const string SectionName = "Advertising";

    public long MaxMediaFileSizeBytes { get; set; } = 20 * 1024 * 1024;

    public string[] AllowedMediaMimeTypes { get; set; } = ["image/jpeg", "image/png", "video/mp4"];

    /// <summary>How long an issued ad-delivery token (RequestAdDeliveryCommand) remains valid — short by design, just enough for a client to receive it and immediately report an impression/click.</summary>
    public int DeliveryTokenLifetimeSeconds { get; set; } = 120;
}
