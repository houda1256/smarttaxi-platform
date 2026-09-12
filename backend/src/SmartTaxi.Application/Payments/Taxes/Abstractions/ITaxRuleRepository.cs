using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.Taxes.Entities;

namespace SmartTaxi.Application.Payments.Taxes.Abstractions;

public interface ITaxRuleRepository
{
    Task AddAsync(TaxRule rule, CancellationToken cancellationToken);

    Task<TaxRule?> GetByIdAsync(Guid taxRuleId, CancellationToken cancellationToken);

    /// <summary>The single active rule (if any) covering this service on this date — never returns more than one, since finalized Invoices must snapshot exactly one rate.</summary>
    Task<TaxRule?> GetApplicableRuleAsync(string applicableService, DateOnly date, CancellationToken cancellationToken);

    Task<PagedResult<TaxRule>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken);

    Task<bool> TryDeactivateAsync(Guid taxRuleId, DateTime utcNow, CancellationToken cancellationToken);
}
