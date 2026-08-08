using SmartTaxi.Domain.Payments.Accounts.Entities;
using SmartTaxi.Domain.Payments.Accounts.Enums;

namespace SmartTaxi.Application.Payments.Ledger.Abstractions;

public interface IFinancialAccountRepository
{
    Task<FinancialAccount?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);

    Task<FinancialAccount?> GetByTypeAndOwnerAsync(FinancialAccountType accountType, Guid? ownerReferenceId, CancellationToken cancellationToken);

    /// <summary>
    /// Accounts are lazily provisioned on first use rather than requiring an
    /// explicit setup step for every Driver/Owner/Partner — this atomically
    /// returns the existing account or opens a new one, safe under
    /// concurrent first-use races (backed by a DB unique index on
    /// (AccountType, OwnerReferenceId)).
    /// </summary>
    Task<FinancialAccount> GetOrCreateAsync(FinancialAccountType accountType, Guid? ownerReferenceId, string currency, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FinancialAccount>> GetForOwnerReferenceAsync(Guid ownerReferenceId, CancellationToken cancellationToken);
}
