using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartTaxi.Application.Identity.Referrals.Abstractions;
using SmartTaxi.Domain.Identity.Referrals.Entities;
using SmartTaxi.Domain.Identity.Referrals.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class ReferralRepository : IReferralRepository
{
    private const string UniqueViolationSqlState = "23505";

    private readonly ApplicationDbContext _context;

    public ReferralRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> AddAsync(Referral referral, CancellationToken cancellationToken)
    {
        await _context.Referrals.AddAsync(referral, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: UniqueViolationSqlState })
        {
            _context.Entry(referral).State = EntityState.Detached;
            return false;
        }
    }

    public Task<Referral?> GetByIdAsync(Guid referralId, CancellationToken cancellationToken) =>
        _context.Referrals.FirstOrDefaultAsync(referral => referral.Id == referralId, cancellationToken);

    public Task<Referral?> GetByRefereeUserIdAsync(Guid refereeUserId, CancellationToken cancellationToken) =>
        _context.Referrals.FirstOrDefaultAsync(referral => referral.RefereeUserId == refereeUserId, cancellationToken);

    public async Task<IReadOnlyCollection<Referral>> GetForReferrerAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
        await _context.Referrals.Where(referral => referral.ReferrerUserId == referrerUserId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Referral>> GetPendingActivationAsync(CancellationToken cancellationToken) =>
        await _context.Referrals
            .Where(referral => referral.Status == ReferralStatus.PendingActivation)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<Referral>> GetRewardEligibleAsync(CancellationToken cancellationToken) =>
        await _context.Referrals
            .Where(referral => referral.Status == ReferralStatus.RewardEligible)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryActivateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Referrals
            .Where(referral => referral.Id == referralId && referral.Status == ReferralStatus.PendingActivation)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(referral => referral.Status, ReferralStatus.RewardEligible)
                .SetProperty(referral => referral.ActivatedAt, utcNow)
                .SetProperty(referral => referral.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryInvalidateAsync(Guid referralId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Referrals
            .Where(referral => referral.Id == referralId && referral.Status != ReferralStatus.Invalidated)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(referral => referral.Status, ReferralStatus.Invalidated)
                .SetProperty(referral => referral.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
