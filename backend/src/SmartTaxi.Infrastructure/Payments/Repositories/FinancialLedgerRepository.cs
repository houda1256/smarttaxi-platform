using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Ledger.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

/// <summary>
/// Posts a batch atomically: the immutable entry rows and every balance-bucket
/// effect they carry commit together or not at all. Idempotency is checked
/// twice — once up front against (SourceType, SourceId) to make the common
/// case a cheap no-op, and again implicitly by the DB's unique
/// (SourceType, SourceId, EntryType) index, which catches a concurrent racer
/// that passed the first check at the same instant.
/// </summary>
internal sealed class FinancialLedgerRepository : IFinancialLedgerRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialLedgerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> PostBatchAsync(
        string sourceType, Guid sourceId, string currency, Guid? createdBy, DateTime utcNow,
        IReadOnlyCollection<LedgerPostingLine> lines, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var alreadyPosted = await _context.FinancialLedgerEntries
            .AnyAsync(entry => entry.SourceType == sourceType && entry.SourceId == sourceId, cancellationToken);

        if (alreadyPosted)
        {
            return false;
        }

        foreach (var line in lines)
        {
            var entry = FinancialLedgerEntry.Post(
                line.DebitAccountId, line.CreditAccountId, line.Amount, currency, line.EntryType,
                sourceType, sourceId, line.Description, createdBy, utcNow);
            await _context.FinancialLedgerEntries.AddAsync(entry, cancellationToken);
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a concurrent race to post the same source between the check above and this insert —
            // the unique (SourceType, SourceId, EntryType) index rejected it.
            return false;
        }

        foreach (var line in lines)
        {
            foreach (var effect in line.DebitEffects)
            {
                await ApplyEffectAsync(line.DebitAccountId, effect, utcNow, cancellationToken);
            }

            foreach (var effect in line.CreditEffects)
            {
                await ApplyEffectAsync(line.CreditAccountId, effect, utcNow, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken) =>
        await _context.FinancialLedgerEntries
            .Where(entry => entry.SourceType == sourceType && entry.SourceId == sourceId)
            .OrderBy(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<FinancialLedgerEntry>> GetForAccountAsync(
        Guid accountId, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        var query = _context.FinancialLedgerEntries
            .Where(entry => entry.DebitAccountId == accountId || entry.CreditAccountId == accountId);

        if (fromUtc is not null)
        {
            query = query.Where(entry => entry.CreatedAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(entry => entry.CreatedAt <= toUtc);
        }

        return await query.OrderBy(entry => entry.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<bool> HasEntriesForSourceAsync(string sourceType, Guid sourceId, CancellationToken cancellationToken) =>
        _context.FinancialLedgerEntries.AnyAsync(entry => entry.SourceType == sourceType && entry.SourceId == sourceId, cancellationToken);

    private Task ApplyEffectAsync(Guid accountId, FinancialBalanceEffect effect, DateTime utcNow, CancellationToken cancellationToken) =>
        effect.Bucket switch
        {
            FinancialBalanceBucket.Pending => _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.PendingBalance, account => account.PendingBalance + effect.Delta)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken),

            FinancialBalanceBucket.Available => _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.AvailableBalance, account => account.AvailableBalance + effect.Delta)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken),

            FinancialBalanceBucket.Reserved => _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.ReservedBalance, account => account.ReservedBalance + effect.Delta)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken),

            FinancialBalanceBucket.PaidOut => _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.PaidOutBalance, account => account.PaidOutBalance + effect.Delta)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken),

            FinancialBalanceBucket.Debt => _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.DebtBalance, account => account.DebtBalance + effect.Delta)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect.Bucket, "Bucket de solde inconnu.")
        };
}
