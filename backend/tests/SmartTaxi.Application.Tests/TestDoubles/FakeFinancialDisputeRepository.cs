using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Enums;
using SmartTaxi.Domain.Payments.Disputes.Entities;
using SmartTaxi.Domain.Payments.Disputes.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

/// <summary>Mirrors the real Infrastructure repository's contract: TryOpenAsync atomically reserves DisputedAmount on the relevant account (Payout's beneficiary account, or Platform otherwise) alongside inserting the dispute row.</summary>
public sealed class FakeFinancialDisputeRepository : IFinancialDisputeRepository
{
    private readonly Dictionary<Guid, FinancialDispute> _disputesById = new();
    private readonly Dictionary<Guid, Guid> _reservedAccountByDisputeId = new();
    private readonly FakeFinancialAccountRepository _accountRepository;
    private readonly FakePayoutRepository _payoutRepository;

    public FakeFinancialDisputeRepository(FakeFinancialAccountRepository accountRepository, FakePayoutRepository payoutRepository)
    {
        _accountRepository = accountRepository;
        _payoutRepository = payoutRepository;
    }

    public Task<FinancialDispute?> GetByIdAsync(Guid disputeId, CancellationToken cancellationToken) =>
        Task.FromResult(_disputesById.GetValueOrDefault(disputeId));

    public Task<PagedResult<FinancialDispute>> GetRaisedByUserAsync(
        Guid raisedBy, FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _disputesById.Values.Where(d => d.RaisedBy == raisedBy);

        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<FinancialDispute>(items, items.Count, pageNumber, pageSize));
    }

    public Task<PagedResult<FinancialDispute>> GetForReviewAsync(
        FinancialDisputeStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _disputesById.Values.AsEnumerable();

        if (status is not null)
        {
            query = query.Where(d => d.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<FinancialDispute>(items, items.Count, pageNumber, pageSize));
    }

    public async Task<bool> TryOpenAsync(FinancialDispute dispute, Guid? createdBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        var accountId = await ResolveAccountIdAsync(dispute, cancellationToken);

        if (accountId is null)
        {
            return false;
        }

        var account = await _accountRepository.GetByIdAsync(accountId.Value, cancellationToken);

        if (account is null || account.AvailableBalance < dispute.DisputedAmount)
        {
            return false;
        }

        _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), account.AvailableBalance - dispute.DisputedAmount);
        _accountRepository.SetProperty(account.Id, nameof(account.ReservedBalance), account.ReservedBalance + dispute.DisputedAmount);

        _disputesById[dispute.Id] = dispute;
        _reservedAccountByDisputeId[dispute.Id] = account.Id;
        return true;
    }

    public Task<bool> TryStartReviewAsync(Guid disputeId, Guid assignedFinanceManagerId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_disputesById.TryGetValue(disputeId, out var dispute) || dispute.Status != FinancialDisputeStatus.Open)
        {
            return Task.FromResult(false);
        }

        SetProperty(dispute, nameof(FinancialDispute.Status), FinancialDisputeStatus.UnderReview);
        SetProperty(dispute, nameof(FinancialDispute.AssignedFinanceManagerId), assignedFinanceManagerId);
        SetProperty(dispute, nameof(FinancialDispute.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryResolveAsync(
        Guid disputeId, string resolution, DisputeResolutionOutcome outcome, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_disputesById.TryGetValue(disputeId, out var dispute) || dispute.Status != FinancialDisputeStatus.UnderReview)
        {
            return Task.FromResult(false);
        }

        var accountId = _reservedAccountByDisputeId[disputeId];
        var account = _accountRepository.GetByIdAsync(accountId, cancellationToken).GetAwaiter().GetResult()!;

        _accountRepository.SetProperty(account.Id, nameof(account.ReservedBalance), account.ReservedBalance - dispute.DisputedAmount);

        if (outcome == DisputeResolutionOutcome.Release)
        {
            _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), account.AvailableBalance + dispute.DisputedAmount);
        }

        SetProperty(dispute, nameof(FinancialDispute.Status), FinancialDisputeStatus.Resolved);
        SetProperty(dispute, nameof(FinancialDispute.Resolution), resolution);
        SetProperty(dispute, nameof(FinancialDispute.ResolvedAt), utcNow);
        SetProperty(dispute, nameof(FinancialDispute.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryRejectAsync(Guid disputeId, string resolution, Guid? resolvedBy, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_disputesById.TryGetValue(disputeId, out var dispute) || dispute.Status != FinancialDisputeStatus.UnderReview)
        {
            return Task.FromResult(false);
        }

        var accountId = _reservedAccountByDisputeId[disputeId];
        var account = _accountRepository.GetByIdAsync(accountId, cancellationToken).GetAwaiter().GetResult()!;

        _accountRepository.SetProperty(account.Id, nameof(account.ReservedBalance), account.ReservedBalance - dispute.DisputedAmount);
        _accountRepository.SetProperty(account.Id, nameof(account.AvailableBalance), account.AvailableBalance + dispute.DisputedAmount);

        SetProperty(dispute, nameof(FinancialDispute.Status), FinancialDisputeStatus.Rejected);
        SetProperty(dispute, nameof(FinancialDispute.Resolution), resolution);
        SetProperty(dispute, nameof(FinancialDispute.ResolvedAt), utcNow);
        SetProperty(dispute, nameof(FinancialDispute.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    public Task<bool> TryEscalateAsync(Guid disputeId, DateTime utcNow, CancellationToken cancellationToken)
    {
        if (!_disputesById.TryGetValue(disputeId, out var dispute) || dispute.Status != FinancialDisputeStatus.UnderReview)
        {
            return Task.FromResult(false);
        }

        SetProperty(dispute, nameof(FinancialDispute.Status), FinancialDisputeStatus.Escalated);
        SetProperty(dispute, nameof(FinancialDispute.UpdatedAt), utcNow);
        return Task.FromResult(true);
    }

    private async Task<Guid?> ResolveAccountIdAsync(FinancialDispute dispute, CancellationToken cancellationToken)
    {
        if (dispute.RelatedPayoutId is { } payoutId)
        {
            var payout = await _payoutRepository.GetByIdAsync(payoutId, cancellationToken);
            return payout?.BeneficiaryAccountId;
        }

        var platformAccount = await _accountRepository.GetOrCreateAsync(FinancialAccountType.Platform, null, dispute.Currency, cancellationToken);
        return platformAccount.Id;
    }

    private static void SetProperty(FinancialDispute dispute, string propertyName, object? value) =>
        typeof(FinancialDispute).GetProperty(propertyName)!.SetValue(dispute, value);
}
