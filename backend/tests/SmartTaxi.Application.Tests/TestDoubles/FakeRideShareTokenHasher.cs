using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeRideShareTokenHasher : IRideShareTokenHasher
{
    private const string Prefix = "hashed:";

    public string Hash(string rawToken) => Prefix + rawToken;
}
