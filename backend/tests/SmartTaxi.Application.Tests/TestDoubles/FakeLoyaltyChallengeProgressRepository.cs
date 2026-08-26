using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyChallengeProgressRepository : ILoyaltyChallengeProgressRepository
{
    private readonly Dictionary<Guid, LoyaltyChallengeProgress> _progress = new();

    public Task<LoyaltyChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken) =>
        Task.FromResult(_progress.Values.FirstOrDefault(p => p.UserId == userId && p.ChallengeId == challengeId));

    public Task<IReadOnlyCollection<LoyaltyChallengeProgress>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<LoyaltyChallengeProgress>>(_progress.Values.Where(p => p.UserId == userId).ToList());

    public Task<bool> TryAddAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken)
    {
        if (_progress.Values.Any(p => p.UserId == progress.UserId && p.ChallengeId == progress.ChallengeId))
        {
            return Task.FromResult(false);
        }

        _progress[progress.Id] = progress;
        return Task.FromResult(true);
    }

    public Task UpdateAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken)
    {
        _progress[progress.Id] = progress;
        return Task.CompletedTask;
    }
}
