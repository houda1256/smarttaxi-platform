using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByReferralCodeAsync(string referralCode, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.ReferralCode == referralCode, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        // The user instance is already tracked by this scoped DbContext when it
        // was loaded via GetByIdAsync, so mutations to it just need SaveChanges.
        return _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecordFailedLoginAttemptAsync(
        Guid userId, int maxAttempts, DateTime utcNow, TimeSpan lockoutDuration, CancellationToken cancellationToken)
    {
        var lockedUntilIfThresholdReached = utcNow.Add(lockoutDuration);

        await _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.FailedLoginAttempts, u => u.FailedLoginAttempts + 1)
                    .SetProperty(
                        u => u.LockedUntilUtc,
                        u => u.FailedLoginAttempts + 1 >= maxAttempts ? lockedUntilIfThresholdReached : u.LockedUntilUtc),
                cancellationToken);

        // ExecuteUpdateAsync itself only returns an affected-row count, not the
        // resulting value, so the new count is read back separately. This is a
        // best-effort read used only to decide whether to emit the AccountLocked
        // audit entry — the lockout enforcement itself stays fully atomic via
        // LockedUntilUtc, which was already set correctly above.
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FailedLoginAttempts)
            .FirstAsync(cancellationToken);
    }

    public Task ResetFailedLoginAttemptsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.FailedLoginAttempts, 0)
                    .SetProperty(u => u.LockedUntilUtc, (DateTime?)null),
                cancellationToken);
    }
}
