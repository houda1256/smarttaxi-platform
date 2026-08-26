using Microsoft.Extensions.Logging;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Infrastructure.Advertising.Services;

internal sealed class AdvertisingMediaStorageCleaner : IAdvertisingMediaStorageCleaner
{
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<AdvertisingMediaStorageCleaner> _logger;

    public AdvertisingMediaStorageCleaner(IFileStorageService fileStorage, ILogger<AdvertisingMediaStorageCleaner> logger)
    {
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task DeleteBestEffortAsync(string storageKey, CancellationToken cancellationToken)
    {
        try
        {
            await _fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Failed to clean up orphaned media object {StorageKey} after a persistence failure — it may remain in storage.", storageKey);
        }
    }
}
