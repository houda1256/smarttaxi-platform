using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeEmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly Dictionary<Guid, EmailVerificationToken> _tokensById = new();

    public Task AddAsync(EmailVerificationToken token, CancellationToken cancellationToken)
    {
        _tokensById[token.Id] = token;
        return Task.CompletedTask;
    }

    public Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(_tokensById.Values.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<DateTime?> GetLastIssuedAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        var lastIssuedAt = _tokensById.Values
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (DateTime?)t.CreatedAt)
            .FirstOrDefault();

        return Task.FromResult(lastIssuedAt);
    }

    public Task InvalidateActiveForUserAsync(Guid userId, DateTime utcNow, CancellationToken cancellationToken)
    {
        foreach (var token in _tokensById.Values.Where(t => t.UserId == userId && t.IsValid(utcNow)))
        {
            ExpireImmediately(token, utcNow);
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryConsumeAsync(Guid tokenId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_tokensById.TryGetValue(tokenId, out var token) || !token.IsValid(utcNow))
        {
            return Task.FromResult(false);
        }

        ConsumeReflectively(token, utcNow);
        return Task.FromResult(true);
    }

    public int Count => _tokensById.Count;

    // EmailVerificationToken has no public mutators (by design — real mutation
    // happens via atomic repository-level SQL in production). The fake uses
    // reflection purely to simulate that same atomicity in memory for tests.
    private static void ExpireImmediately(EmailVerificationToken token, DateTime utcNow) =>
        typeof(EmailVerificationToken).GetProperty(nameof(EmailVerificationToken.ExpiresAt))!
            .SetValue(token, utcNow);

    private static void ConsumeReflectively(EmailVerificationToken token, DateTime utcNow) =>
        typeof(EmailVerificationToken).GetProperty(nameof(EmailVerificationToken.ConsumedAt))!
            .SetValue(token, utcNow);
}
