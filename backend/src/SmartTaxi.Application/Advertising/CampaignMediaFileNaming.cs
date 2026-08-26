namespace SmartTaxi.Application.Advertising;

/// <summary>The stored file name and storage key never come from client input — the extension is derived only from the validated MIME type, and the display name is sanitized/length-capped purely for later display, never used to build a path.</summary>
public static class CampaignMediaFileNaming
{
    private static readonly IReadOnlyDictionary<string, string> ExtensionsByMimeType = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["video/mp4"] = ".mp4"
    };

    public static string GetExtensionForMimeType(string mimeType) => ExtensionsByMimeType.GetValueOrDefault(mimeType, string.Empty);

    public static string SanitizeDisplayFileName(string originalFileName)
    {
        var name = string.IsNullOrWhiteSpace(originalFileName) ? "media" : originalFileName.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        return cleaned.Length > 255 ? cleaned[..255] : cleaned;
    }

    public static string CreateStorageKey(Guid campaignId, string mimeType) => $"advertising/{campaignId}/{Guid.NewGuid()}{GetExtensionForMimeType(mimeType)}";
}
