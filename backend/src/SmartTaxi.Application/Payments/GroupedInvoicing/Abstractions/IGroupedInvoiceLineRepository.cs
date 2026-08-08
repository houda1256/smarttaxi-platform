using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;

public interface IGroupedInvoiceLineRepository
{
    Task<bool> AnyAlreadyInvoicedAsync(IReadOnlyCollection<Guid> rideIds, CancellationToken cancellationToken);

    /// <summary>
    /// Guarded by a DB unique index on RideId (real implementation): if any of
    /// the given Rides was already invoiced by the time this actually writes —
    /// even if AnyAlreadyInvoicedAsync's earlier check missed a concurrent
    /// race — the whole batch is rejected, never partially inserted.
    /// </summary>
    Task<bool> TryAddRangeAsync(IReadOnlyCollection<GroupedInvoiceLine> lines, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<GroupedInvoiceLine>> GetForInvoiceAsync(Guid groupedInvoiceId, CancellationToken cancellationToken);
}
