using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideShareTokenGenerator : IRideShareTokenGenerator
{
    private int _counter;

    public string Generate() => $"raw-share-token-{Interlocked.Increment(ref _counter)}";
}
