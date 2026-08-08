namespace SmartTaxi.Application.Identity.Documents.Abstractions;

/// <summary>
/// Stores/retrieves document bytes outside the database. The storage key is
/// an opaque reference (never a client-supplied name, never a physical path
/// exposed to callers) — only Infrastructure's implementation knows how a key
/// maps to a real location.
/// </summary>
public interface IFileStorageService
{
    Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
}
