using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyChallengeProgressRepository : ILoyaltyChallengeProgressRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyChallengeProgressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyChallengeProgress?> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken) =>
        _context.LoyaltyChallengeProgresses.FirstOrDefaultAsync(
            progress => progress.UserId == userId && progress.ChallengeId == challengeId, cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyChallengeProgress>> GetForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await _context.LoyaltyChallengeProgresses.Where(progress => progress.UserId == userId).ToListAsync(cancellationToken);

    public async Task<bool> TryAddAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken)
    {
        await _context.LoyaltyChallengeProgresses.AddAsync(progress, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(progress).State = EntityState.Detached;
            return false;
        }
    }

    public async Task UpdateAsync(LoyaltyChallengeProgress progress, CancellationToken cancellationToken)
    {
        _context.LoyaltyChallengeProgresses.Update(progress);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
