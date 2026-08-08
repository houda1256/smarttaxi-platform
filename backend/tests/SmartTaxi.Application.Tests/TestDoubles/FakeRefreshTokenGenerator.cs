using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
{
    private int _counter;

    public string? LastGenerated { get; private set; }

    public string Generate()
    {
        LastGenerated = $"raw-refresh-token-{Interlocked.Increment(ref _counter)}";
        return LastGenerated;
    }
}
