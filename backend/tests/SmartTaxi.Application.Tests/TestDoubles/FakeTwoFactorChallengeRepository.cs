using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeTwoFactorChallengeRepository : ITwoFactorChallengeRepository
{
    private readonly Dictionary<Guid, TwoFactorChallenge> _challengesById = new();

    public Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken)
    {
        _challengesById[challenge.Id] = challenge;
        return Task.CompletedTask;
    }

    public Task<TwoFactorChallenge?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(_challengesById.Values.FirstOrDefault(c => c.TokenHash == tokenHash));

    public Task<bool> TryConsumeAsync(Guid challengeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_challengesById.TryGetValue(challengeId, out var challenge) || !challenge.IsValid(utcNow))
        {
            return Task.FromResult(false);
        }

        typeof(TwoFactorChallenge).GetProperty(nameof(TwoFactorChallenge.ConsumedAt))!.SetValue(challenge, utcNow);
        return Task.FromResult(true);
    }

    public int Count => _challengesById.Count;
}
