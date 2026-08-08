using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

/// <summary>
/// Composes the FinancialAccounts and FinancialDisputes tables directly
/// (rather than delegating to FinancialAccountRepository, which opens its own
/// transaction) so the account's Available/Reserved rebucketing and the
/// dispute row/status commit as one atomic unit — mirrors PayoutRepository's
/// TryCompleteAsync pattern exactly.
/// </summary>
internal sealed class FinancialDisputeRepository : IFinancialDisputeRepository
{
    private readonly ApplicationDbContext _context;

    public FinancialDisputeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<FinancialDispute?> GetByIdAsync(Guid disputeId, CancellationToken cancellationToken) =>
        _context.FinancialDisputes.FirstOrDefaultAsync(dispute => dispute.Id == disputeId, cancellationToken);

    public async Task<PagedResult<FinancialDispute>> GetRaisedByUserAsync(
        Guid raisedBy, FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.FinancialDisputes.Where(dispute => dispute.RaisedBy == raisedBy);

        if (status is not null)
        {
            query = query.Where(dispute => dispute.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(dispute => dispute.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancialDispute>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<FinancialDispute>> GetForReviewAsync(
        FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.FinancialDisputes.AsQueryable();

        if (status is not null)
        {
            query = query.Where(dispute => dispute.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(dispute => dispute.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FinancialDispute>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<bool> TryOpenAsync(FinancialDispute dispute, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var accountId = await ResolveOrCreateAccountIdAsync(dispute, cancellationToken);

        if (accountId is null)
        {
            return false;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var accountRows = await _context.FinancialAccounts
            .Where(account => account.Id == accountId && account.AvailableBalance >= dispute.DisputedAmount)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(account => account.AvailableBalance, account => account.AvailableBalance - dispute.DisputedAmount)
                .SetProperty(account => account.ReservedBalance, account => account.ReservedBalance + dispute.DisputedAmount)
                .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken);

        if (accountRows != 1)
        {
            return false;
        }

        await _context.FinancialDisputes.AddAsync(dispute, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryStartReviewAsync(Guid disputeId, Guid assignedFinanceManagerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.FinancialDisputes
            .Where(dispute => dispute.Id == disputeId && dispute.Status == FinancialDisputeStatus.Open)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(dispute => dispute.Status, FinancialDisputeStatus.UnderReview)
                .SetProperty(dispute => dispute.AssignedFinanceManagerId, assignedFinanceManagerId)
                .SetProperty(dispute => dispute.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryResolveAsync(
        Guid disputeId, string resolution, DisputeResolutionOutcome outcome, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var dispute = await _context.FinancialDisputes.FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null || dispute.Status != FinancialDisputeStatus.UnderReview)
        {
            return false;
        }

        var accountId = await ResolveAccountIdAsync(dispute, cancellationToken);

        if (accountId is null)
        {
            return false;
        }

        await ReleaseReservationAsync(accountId.Value, dispute.DisputedAmount, outcome == DisputeResolutionOutcome.Release, utcNow, cancellationToken);

        var rows = await _context.FinancialDisputes
            .Where(d => d.Id == disputeId && d.Status == FinancialDisputeStatus.UnderReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, FinancialDisputeStatus.Resolved)
                .SetProperty(d => d.Resolution, resolution)
                .SetProperty(d => d.ResolvedAt, utcNow)
                .SetProperty(d => d.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryRejectAsync(Guid disputeId, string resolution, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var dispute = await _context.FinancialDisputes.FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);

        if (dispute is null || dispute.Status != FinancialDisputeStatus.UnderReview)
        {
            return false;
        }

        var accountId = await ResolveAccountIdAsync(dispute, cancellationToken);

        if (accountId is null)
        {
            return false;
        }

        // A rejected dispute means the original transaction stands — the reservation always releases.
        await ReleaseReservationAsync(accountId.Value, dispute.DisputedAmount, releaseToAvailable: true, utcNow, cancellationToken);

        var rows = await _context.FinancialDisputes
            .Where(d => d.Id == disputeId && d.Status == FinancialDisputeStatus.UnderReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Status, FinancialDisputeStatus.Rejected)
                .SetProperty(d => d.Resolution, resolution)
                .SetProperty(d => d.ResolvedAt, utcNow)
                .SetProperty(d => d.UpdatedAt, utcNow), cancellationToken);

        if (rows != 1)
        {
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryEscalateAsync(Guid disputeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.FinancialDisputes
            .Where(dispute => dispute.Id == disputeId && dispute.Status == FinancialDisputeStatus.UnderReview)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(dispute => dispute.Status, FinancialDisputeStatus.Escalated)
                .SetProperty(dispute => dispute.UpdatedAt, utcNow), cancellationToken);

        return rows == 1;
    }

    private Task ReleaseReservationAsync(Guid accountId, decimal amount, bool releaseToAvailable, DateTime utcNow, CancellationToken cancellationToken) =>
        releaseToAvailable
            ? _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.ReservedBalance, account => account.ReservedBalance - amount)
                    .SetProperty(account => account.AvailableBalance, account => account.AvailableBalance + amount)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken)
            : _context.FinancialAccounts
                .Where(account => account.Id == accountId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(account => account.ReservedBalance, account => account.ReservedBalance - amount)
                    .SetProperty(account => account.UpdatedAt, utcNow), cancellationToken);

    /// <summary>Read-only lookup for an already-open dispute — the account must already exist by resolution time.</summary>
    private async Task<Guid?> ResolveAccountIdAsync(FinancialDispute dispute, CancellationToken cancellationToken)
    {
        if (dispute.RelatedPayoutId is { } payoutId)
        {
            var payout = await _context.Payouts.FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken);
            return payout?.BeneficiaryAccountId;
        }

        var platformAccount = await _context.FinancialAccounts
            .FirstOrDefaultAsync(account => account.AccountType == FinancialAccountType.Platform, cancellationToken);
        return platformAccount?.Id;
    }

    /// <summary>Same as ResolveAccountIdAsync, but lazily provisions the Platform account if it doesn't exist yet (mirrors FinancialAccountRepository.GetOrCreateAsync's own race-safe pattern).</summary>
    private async Task<Guid?> ResolveOrCreateAccountIdAsync(FinancialDispute dispute, CancellationToken cancellationToken)
    {
        if (dispute.RelatedPayoutId is { } payoutId)
        {
            var payout = await _context.Payouts.FirstOrDefaultAsync(p => p.Id == payoutId, cancellationToken);
            return payout?.BeneficiaryAccountId;
        }

        var existing = await _context.FinancialAccounts
            .FirstOrDefaultAsync(account => account.AccountType == FinancialAccountType.Platform, cancellationToken);

        if (existing is not null)
        {
            return existing.Id;
        }

        var created = FinancialAccount.Open(FinancialAccountType.Platform, null, dispute.Currency, DateTime.UtcNow);
        await _context.FinancialAccounts.AddAsync(created, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return created.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(created).State = EntityState.Detached;
            var raced = await _context.FinancialAccounts
                .FirstOrDefaultAsync(account => account.AccountType == FinancialAccountType.Platform, cancellationToken);
            return raced?.Id;
        }
    }
}
