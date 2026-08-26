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

    /// <summary>
    /// Deletes the object at storageKey, if present — a no-op (never throws
    /// NotFound-style errors) if it does not exist. Generic storage capability,
    /// not module-specific: any caller performing compensating cleanup after a
    /// failed persist-after-save sequence (see Module 8's orphaned-media fix)
    /// needs this, not just Advertising.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
