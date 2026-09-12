using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class TwoFactorChallengeRepository : ITwoFactorChallengeRepository
{
    private readonly ApplicationDbContext _context;

    public TwoFactorChallengeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TwoFactorChallenge challenge, CancellationToken cancellationToken)
    {
        await _context.TwoFactorChallenges.AddAsync(challenge, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<TwoFactorChallenge?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _context.TwoFactorChallenges.FirstOrDefaultAsync(challenge => challenge.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> TryConsumeAsync(Guid challengeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.TwoFactorChallenges
            .Where(challenge => challenge.Id == challengeId && challenge.ConsumedAt == null && challenge.ExpiresAt > utcNow)
            .ExecuteUpdateAsync(setters => setters.SetProperty(challenge => challenge.ConsumedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
