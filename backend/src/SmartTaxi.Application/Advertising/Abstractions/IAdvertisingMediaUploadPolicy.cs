namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>Admin-configurable creative upload limits — same "policy abstraction, never a hardcoded constant" convention as IDocumentUploadPolicy.</summary>
public interface IAdvertisingMediaUploadPolicy
{
    long MaxFileSizeBytes { get; }

    IReadOnlyCollection<string> AllowedMimeTypes { get; }
}
