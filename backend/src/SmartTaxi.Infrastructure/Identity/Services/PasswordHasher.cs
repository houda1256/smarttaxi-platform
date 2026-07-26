using Microsoft.AspNetCore.Identity;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string rawPassword)
    {
        return _hasher.HashPassword(default!, rawPassword);
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(default!, hashedPassword, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
