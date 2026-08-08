namespace SmartTaxi.Infrastructure.Identity.Options;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "DocumentStorage";

    public string RootPath { get; init; } = "storage/documents";

    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public string[] AllowedMimeTypes { get; init; } = ["application/pdf", "image/jpeg", "image/png"];
}
