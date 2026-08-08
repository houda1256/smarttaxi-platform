using SmartTaxi.Application.Identity.Documents.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFileStorageService : IFileStorageService
{
    private readonly Dictionary<string, byte[]> _contentByKey = new();

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

    public int SavedFileCount => _contentByKey.Count;
}
