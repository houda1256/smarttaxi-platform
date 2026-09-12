using System.Security.Cryptography;
using System.Text;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class RefreshTokenHasher : IRefreshTokenHasher
{
    // SHA-256 (fast, deterministic) is deliberately used instead of the slow,
    // adaptive password hasher: the refresh token already carries 256 bits of
    // cryptographic entropy from RefreshTokenGenerator, so a slow hash would
    // only add latency to every refresh call without any security benefit.
    public string Hash(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
