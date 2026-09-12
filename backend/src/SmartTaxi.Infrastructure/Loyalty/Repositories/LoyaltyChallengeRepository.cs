using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Loyalty.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Loyalty.Repositories;

internal sealed class LoyaltyChallengeRepository : ILoyaltyChallengeRepository
{
    private readonly ApplicationDbContext _context;

    public LoyaltyChallengeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<LoyaltyChallenge?> GetByIdAsync(Guid challengeId, CancellationToken cancellationToken) =>
        _context.LoyaltyChallenges.FirstOrDefaultAsync(challenge => challenge.Id == challengeId, cancellationToken);

    public Task<LoyaltyChallenge?> GetByCodeAsync(string code, CancellationToken cancellationToken) =>
        _context.LoyaltyChallenges.FirstOrDefaultAsync(challenge => challenge.Code == code.Trim().ToUpper(), cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyChallenge>> GetAllAsync(CancellationToken cancellationToken) =>
        await _context.LoyaltyChallenges.OrderBy(challenge => challenge.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<LoyaltyChallenge>> GetActiveForRoleAsync(
        UserRole role, DateTime utcNow, CancellationToken cancellationToken)
    {
        var candidates = await _context.LoyaltyChallenges.Where(challenge => challenge.IsActive).ToListAsync(cancellationToken);
        return candidates.Where(challenge => challenge.IsEffectiveFor(role, utcNow)).ToList();
    }

    public async Task AddAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken)
    {
        await _context.LoyaltyChallenges.AddAsync(challenge, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LoyaltyChallenge challenge, CancellationToken cancellationToken)
    {
        _context.LoyaltyChallenges.Update(challenge);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
