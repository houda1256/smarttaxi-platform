using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Entities;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

/// <summary>
/// TryCompleteAsync is the sole place a Payout's money actually moves, and it
/// composes the FinancialAccounts and FinancialLedgerEntries tables directly
/// (rather than delegating to FinancialAccountRepository/FinancialLedgerRepository,
/// which each open their own transaction) so the balance move, the Payout
/// status transition, and the audit ledger entry commit as one atomic unit —
/// mirrors PaymentRepository's TryConfirmAsync/TryApplyRefundAsync pattern.
/// </summary>
internal sealed class PayoutRepository : IPayoutRepository
{
    private const string PayoutSourceType = "Payout";

    private static readonly PayoutStatus[] CancellableStatuses =
        [PayoutStatus.Requested, PayoutStatus.PendingApproval, PayoutStatus.Approved];

    private readonly ApplicationDbContext _context;

    public PayoutRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payout payout, CancellationToken cancellationToken)
    {
        await _context.Payouts.AddAsync(payout, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Payout?> GetByIdAsync(Guid payoutId, CancellationToken cancellationToken) =>
        _context.Payouts.FirstOrDefaultAsync(payout => payout.Id == payoutId, cancellationToken);

    public async Task<PagedResult<Payout>> GetForBeneficiaryAccountAsync(
        Guid beneficiaryAccountId, PayoutStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Payouts.Where(payout => payout.BeneficiaryAccountId == beneficiaryAccountId);

        if (status is not null)
        {
            query = query.Where(payout => payout.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(payout => payout.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payout>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<Payout>> GetForAdminAsync(
        FinancialAccountType? beneficiaryType, PayoutStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Payouts.AsQueryable();

        if (beneficiaryType is not null)
        {
            query = query.Where(payout => payout.BeneficiaryType == beneficiaryType);
        }

        if (status is not null)
        {
            query = query.Where(payout => payout.Status == status);
        }

        if (fromUtc is not null)
        {
            query = query.Where(payout => payout.RequestedAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(payout => payout.RequestedAt <= toUtc);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(payout => payout.RequestedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Payout>(items, totalCount, pageNumber, pageSize);
    }

    public Task<bool> TrySubmitForApprovalAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(payoutId, PayoutStatus.Requested, PayoutStatus.PendingApproval, utcNow, cancellationToken);

    public async Task<bool> TryApproveAsync(Guid payoutId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Payouts
            .Where(payout => payout.Id == payoutId && payout.Status == PayoutStatus.PendingApproval)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payout => payout.Status, PayoutStatus.Approved)
                .SetProperty(payout => payout.ApprovedBy, approvedBy)
                .SetProperty(payout => payout.ApprovedAt, utcNow)
                .SetProperty(payout => payout.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public Task<bool> TryRejectAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransitionAsync(payoutId, PayoutStatus.PendingApproval, PayoutStatus.Rejected, utcNow, cancellationToken);

    public async Task<bool> TryStartProcessingAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Payouts
            .Where(payout => payout.Id == payoutId && payout.Status == PayoutStatus.Approved)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payout => payout.Status, PayoutStatus.Processing)
                .SetProperty(payout => payout.ProcessedAt, utcNow)
                .SetProperty(payout => payout.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryCancelAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Payouts
            .Where(payout => payout.Id == payoutId && CancellableStatuses.Contains(payout.Status))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payout => payout.Status, PayoutStatus.Cancelled)
                .SetProperty(payout => payout.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryMarkFailedAsync(Guid payoutId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Payouts
            .Where(payout => payout.Id == payoutId && payout.Status == PayoutStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payout => payout.Status, PayoutStatus.Failed)
                .SetProperty(payout => payout.FailureReason, reason)
                .SetProperty(payout => payout.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<PayoutCompletionOutcome> TryCompleteAsync(Guid payoutId, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var payout = await _context.Payouts.FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken);

        if (payout is null || payout.Status != PayoutStatus.Processing)
        {
            return PayoutCompletionOutcome.NotInProcessingState;
        }

        var accountRows = await _context.FinancialAccounts
            .Where(account => account.Id == payout.BeneficiaryAccountId && account.AvailableBalance >= payout.Amount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(account => account.AvailableBalance, account => account.AvailableBalance - payout.Amount)
                .SetProperty(account => account.PaidOutBalance, account => account.PaidOutBalance + payout.Amount)
                .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken);

        if (accountRows != 1)
        {
            return PayoutCompletionOutcome.InsufficientAvailableBalance;
        }

        var payoutRows = await _context.Payouts
            .Where(p => p.Id == payoutId && p.Status == PayoutStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, PayoutStatus.Paid)
                .SetProperty(p => p.PaidAt, utcNow)
                .SetProperty(p => p.UpdatedAt, utcNow), cancellationToken);

        if (payoutRows != 1)
        {
            // Lost a concurrent race (another completer, or an admin cancel/fail, won first) — the
            // transaction is never committed, so the balance move above is rolled back too.
            return PayoutCompletionOutcome.NotInProcessingState;
        }

        var alreadyPosted = await _context.FinancialLedgerEntries
            .AnyAsync(entry => entry.SourceType == PayoutSourceType && entry.SourceId == payoutId, cancellationToken);

        if (!alreadyPosted)
        {
            var entry = FinancialLedgerEntry.Post(
                payout.BeneficiaryAccountId, payout.BeneficiaryAccountId, payout.Amount, payout.Currency, LedgerEntryType.Payout,
                PayoutSourceType, payoutId, "Versement", createdBy, utcNow);
            await _context.FinancialLedgerEntries.AddAsync(entry, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return PayoutCompletionOutcome.Completed;
    }

    private async Task<bool> TryTransitionAsync(Guid payoutId, PayoutStatus from, PayoutStatus to, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.Payouts
            .Where(payout => payout.Id == payoutId && payout.Status == from)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payout => payout.Status, to)
                .SetProperty(payout => payout.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }
}
