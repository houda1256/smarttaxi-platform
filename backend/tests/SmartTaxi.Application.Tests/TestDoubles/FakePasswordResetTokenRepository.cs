using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakePasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly Dictionary<Guid, PasswordResetToken> _tokensById = new();

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken)
    {
        _tokensById[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(_tokensById.Values.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        foreach (var token in _tokensById.Values.Where(t => t.UserId == userId && t.IsValid(utcNow)))
        {
            typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ExpiresAt))!.SetValue(token, utcNow);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tokensById.TryGetValue(tokenId, out var token) || !token.IsValid(utcNow))
        {
            return Task.FromResult(false);
        }

        typeof(PasswordResetToken).GetProperty(nameof(PasswordResetToken.ConsumedAt))!.SetValue(token, utcNow);
        return Task.FromResult(true);
    }

    public int Count => _tokensById.Count;
}
