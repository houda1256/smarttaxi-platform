using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyChallengeRepository : ILoyaltyChallengeRepository
{
    private readonly Dictionary<Guid, LoyaltyChallenge> _challenges = new();

    public Task<LoyaltyChallenge?> GetByIdAsync(Guid challengeId, CancellationToken cancellationToken) =>
        Task.FromResult(_challenges.GetValueOrDefault(challengeId));

    public Task<LoyaltyChallenge?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        Task.FromResult(_challenges.Values.FirstOrDefault(c => c.Code == code.Trim().ToUpper()));

    public Task<IReadOnlyCollection<LoyaltyChallenge>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyChallenge>>(_challenges.Values.ToList());

    public Task<IReadOnlyCollection<LoyaltyChallenge>> GetActiveForRoleAsync(UserRole role, DateTime utcNow, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyChallenge>>(_challenges.Values.Where(c => c.IsEffectiveFor(role, utcNow)).ToList());

    public Task AddAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken)
    {
        _challenges[challenge.Id] = challenge;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken)
    {
        _challenges[challenge.Id] = challenge;
        return Task.CompletedTask;
    }
}
