using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Ledger.Enums;
using SmartTaxi.Domain.Payments.Payouts.Entities;
using SmartTaxi.Domain.Payments.Payouts.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>
/// TryCompleteAsync mirrors the real Infrastructure repository's contract: it
/// atomically (single-threaded here, but same guard order) re-verifies the
/// Payout is Processing AND the beneficiary account still has enough
/// AvailableBalance before moving Available -> PaidOut and posting the
/// traceable ledger entry — all or nothing.
/// </summary>
public sealed class FakePayoutRepository : IPayoutRepository
{
    private readonly Dictionary<Guid, Payout> _payoutsById = new();
    private readonly FakeFinancialAccountRepository _accountRepository;
    private readonly FakeFinancialLedgerRepository _ledgerRepository;

    public FakePayoutRepository(FakeFinancialAccountRepository accountRepository, FakeFinancialLedgerRepository ledgerRepository)
    {
        _accountRepository = accountRepository;
        _ledgerRepository = ledgerRepository;
    }

    public Task AddAsync(Payout payout, CancellationToken cancellationToken)
    {
        _payoutsById[payout.Id] = payout;
        return Task.CompletedTask;
    }

    public Task<Payout?> GetByIdAsync(Guid payoutId, CancellationToken cancellationToken) =>
        Task.FromResult(_payoutsById.GetValueOrDefault(payoutId));

    public Task<PagedResult<Payout>> GetForBeneficiaryAccountAsync(
        Guid beneficiaryAccountId, PayoutStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _payoutsById.Values.Where(p => p.BeneficiaryAccountId == beneficiaryAccountId);

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<Payout>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<Payout>> GetForAdminAsync(
        FinancialAccountType? beneficiaryType, PayoutStatus? status, DateTime? fromUtc, DateTime? toUtc,
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _payoutsById.Values.AsEnumerable();

        if (beneficiaryType is not null)
        {
            query = query.Where(p => p.BeneficiaryType == beneficiaryType);
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status);
        }

        if (fromUtc is not null)
        {
            query = query.Where(p => p.RequestedAt >= fromUtc);
        }

        if (toUtc is not null)
        {
            query = query.Where(p => p.RequestedAt <= toUtc);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<Payout>(items, items.Count, pageNumber, pageSize));
    }

    public Task<bool> TrySubmitForApprovalAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(payoutId, PayoutStatus.Requested, PayoutStatus.PendingApproval, utcNow);

    public Task<bool> TryApproveAsync(Guid payoutId, Guid approvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout) || payout.Status != PayoutStatus.PendingApproval)
        {
            return Task.FromResult(false);
        }

        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Approved);
        SetProperty(payout, nameof(Payout.ApprovedBy), approvedBy);
        SetProperty(payout, nameof(Payout.ApprovedAt), utcNow);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(payoutId, PayoutStatus.PendingApproval, PayoutStatus.Rejected, utcNow);

    public Task<bool> TryStartProcessingAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout) || payout.Status != PayoutStatus.Approved)
        {
            return Task.FromResult(false);
        }

        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Processing);
        SetProperty(payout, nameof(Payout.ProcessedAt), utcNow);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryCancelAsync(Guid payoutId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout)
            || payout.Status is not (PayoutStatus.Requested or PayoutStatus.PendingApproval or PayoutStatus.Approved))
        {
            return Task.FromResult(false);
        }

        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Cancelled);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryMarkFailedAsync(Guid payoutId, string reason, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout) || payout.Status != PayoutStatus.Processing)
        {
            return Task.FromResult(false);
        }

        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Failed);
        SetProperty(payout, nameof(Payout.FailureReason), reason);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public async Task<PayoutCompletionOutcome> TryCompleteAsync(Guid payoutId, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout) || payout.Status != PayoutStatus.Processing)
        {
            return PayoutCompletionOutcome.NotInProcessingState;
        }

        var account = await _accountRepository.GetByIdAsync(payout.BeneficiaryAccountId, cancellationToken);

        if (account is null || account.AvailableBalance < payout.Amount)
        {
            return PayoutCompletionOutcome.InsufficientAvailableBalance;
        }

        _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), account.AvailableBalance - payout.Amount);
        _accountRepository.SetProperty(account.Id, nameof(account.PaidOutBalance), account.PaidOutBalance + payout.Amount);

        SetProperty(payout, nameof(Payout.Status), PayoutStatus.Paid);
        SetProperty(payout, nameof(Payout.PaidAt), utcNow);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);

        var line = new LedgerPostingLine(
            account.Id, account.Id, payout.Amount, LedgerEntryType.Payout, "Versement",
            DebitEffects: [], CreditEffects: []);
        await _ledgerRepository.PostBatchAsync("Payout", payoutId, payout.Currency, createdBy, utcNow, [line], cancellationToken);

        return PayoutCompletionOutcome.Completed;
    }

    private Task<bool> TryTransition(Guid payoutId, PayoutStatus from, PayoutStatus to, DateTime utcNow)
    {
        if (!_payoutsById.TryGetValue(payoutId, out var payout) || payout.Status != from)
        {
            return Task.FromResult(false);
        }

        SetProperty(payout, nameof(Payout.Status), to);
        SetProperty(payout, nameof(Payout.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private static void SetProperty(Payout payout, string propertyName, object? value) =>
        typeof(Payout).GetProperty(propertyName)!.SetValue(payout, value);
}
