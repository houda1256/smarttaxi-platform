namespace SmartTaxi.Application.Advertising.Abstractions;

/// <summary>
/// Best-effort compensating cleanup for a creative's storage object when the
/// DB persistence step after a successful upload fails — see Module 8 audit's
/// orphaned-media finding. Lives as its own seam (rather than a plain
/// IFileStorageService.DeleteAsync call inline in the handler) because
/// logging a cleanup failure needs ILogger, which the Application project
/// cannot reference (zero NuGet packages, same rule as
/// NotificationDispatcher/JwtTokenGenerator) — the Infrastructure
/// implementation owns that. Must NEVER throw: a cleanup failure must not
/// mask the original DB exception the caller is already propagating.
/// </summary>
public interface IAdvertisingMediaStorageCleaner
{
    Task DeleteBestEffortAsync(string storageKey, CancellationToken cancellationToken);
}
