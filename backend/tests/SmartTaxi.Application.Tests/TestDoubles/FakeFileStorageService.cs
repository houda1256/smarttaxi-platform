using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _contentByKey = new();

    /// <summary>Test hook: when set, DeleteAsync throws — used to prove a cleanup failure never masks the original DB exception.</summary>
    public bool ThrowOnDelete { get; set; }

    public List<string> DeletedKeys { get; } = [];

    public async Task SaveAsync(string storageKey, Stream content, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _contentByKey[storageKey] = buffer.ToArray();
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        var bytes = _contentByKey.GetValueOrDefault(storageKey) ?? [];
        return Task.FromResult<Stream>(new MemoryStream(bytes));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        if (ThrowOnDelete)
        {
            throw new InvalidOperationException("Simulated delete failure (test).");
        }

        _contentByKey.Remove(storageKey);
        DeletedKeys.Add(storageKey);
        return Task.CompletedTask;
    }

    public bool Contains(string storageKey) => _contentByKey.ContainsKey(storageKey);

    public int SavedFileCount => _contentByKey.Count;
}
