using SmartTaxi.Application.Payments.Ledger.Abstractions;
using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeFinancialAccountRepository : IFinancialAccountRepository
{
    private readonly Dictionary<Guid, FinancialAccount> _accountsById = new();

    public Task<FinancialAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult(_accountsById.GetValueOrDefault(accountId));

    public Task<FinancialAccount?> GetByTypeAndOwnerAsync(FinancialAccountType accountType, Guid? ownerReferenceId, CancellationToken cancellationToken)
    {
        var account = _accountsById.Values.FirstOrDefault(a => a.AccountType == accountType && a.OwnerReferenceId == ownerReferenceId);
        return Task.FromResult(account);
    }

    public Task<FinancialAccount> GetOrCreateAsync(FinancialAccountType accountType, Guid? ownerReferenceId, string currency, CancellationToken cancellationToken)
    {
        var existing = _accountsById.Values.FirstOrDefault(a => a.AccountType == accountType && a.OwnerReferenceId == ownerReferenceId);

        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var created = FinancialAccount.Open(accountType, ownerReferenceId, currency, DateTime.UtcNow);
        _accountsById[created.Id] = created;
        return Task.FromResult(created);
    }

    public Task<IReadOnlyCollection<FinancialAccount>> GetForOwnerReferenceAsync(Guid ownerReferenceId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<FinancialAccount> accounts = _accountsById.Values.Where(a => a.OwnerReferenceId == ownerReferenceId).ToList();
        return Task.FromResult(accounts);
    }

    public void SetProperty(Guid accountId, string propertyName, object? value)
    {
        var account = _accountsById[accountId];
        typeof(FinancialAccount).GetProperty(propertyName)!.SetValue(account, value);
    }
}
