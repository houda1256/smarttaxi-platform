using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Infrastructure.Rides.Services;

internal sealed class RideShareTokenHasher : IRideShareTokenHasher
{
    // SHA-256 (fast, deterministic) mirrors RefreshTokenHasher's reasoning: the
    // token already carries 256 bits of entropy from RideShareTokenGenerator,
    // so a slow adaptive hash would only add latency with no security benefit.
    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
