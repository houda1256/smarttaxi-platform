using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;

public interface IGroupedInvoiceRepository
{
    Task AddAsync(GroupedInvoice invoice, CancellationToken cancellationToken);

    Task<GroupedInvoice?> GetByIdAsync(Guid groupedInvoiceId, CancellationToken cancellationToken);

    Task<PagedResult<GroupedInvoice>> GetForBusinessCustomerAsync(
        Guid businessCustomerId, GroupedInvoiceStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken);

    /// <summary>Computed, not stored — Issued invoices whose DueDate has passed as of the given instant. No background job flips a stored status, so a tax/rate change can never retroactively affect what "overdue" means for a finalized invoice.</summary>
    Task<IReadOnlyCollection<GroupedInvoice>> GetOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken);

    Task<bool> TryMarkPaidAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken);

    Task<bool> TryCancelAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken);
}
