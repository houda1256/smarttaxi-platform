using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Mirrors the real Infrastructure implementation's shape (delegates to IFileStorageService.DeleteAsync,
/// swallows every exception) without needing an ILogger dependency in tests.</summary>
public sealed class FakeAdvertisingMediaStorageCleaner : IAdvertisingMediaStorageCleaner
{
    private readonly IFileStorageService _fileStorage;

    public List<string> AttemptedDeletes { get; } = [];

    public FakeAdvertisingMediaStorageCleaner(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task DeleteBestEffortAsync(string storageKey, CancellationToken cancellationToken)
    {
        AttemptedDeletes.Add(storageKey);

        try
        {
            await _fileStorage.DeleteAsync(storageKey, cancellationToken);
        }
        catch
        {
            // Best-effort — never rethrow, matching the real implementation's contract.
        }
    }
}
