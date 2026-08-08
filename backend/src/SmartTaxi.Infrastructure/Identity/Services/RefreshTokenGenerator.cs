using System.Security.Cryptography;
using SmartTaxi.Application.Identity.Abstractions;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeInBytes = 32; // 256 bits of entropy

    public string Generate() => Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenSizeInBytes));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
