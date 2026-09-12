using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeGroupedInvoiceLineRepository : IGroupedInvoiceLineRepository
{
    private readonly List<GroupedInvoiceLine> _lines = [];

    public Task<bool> AnyAlreadyInvoicedAsync(IReadOnlyCollection<Guid> rideIds, CancellationToken cancellationToken) =>
        Task.FromResult(_lines.Any(l => rideIds.Contains(l.RideId)));

    public Task<bool> TryAddRangeAsync(IReadOnlyCollection<GroupedInvoiceLine> lines, CancellationToken cancellationToken)
    {
        var rideIds = lines.Select(l => l.RideId).ToList();

        if (_lines.Any(l => rideIds.Contains(l.RideId)))
        {
            return Task.FromResult(false);
        }

        _lines.AddRange(lines);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyCollection<GroupedInvoiceLine>> GetForInvoiceAsync(Guid groupedInvoiceId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GroupedInvoiceLine> lines = _lines.Where(l => l.GroupedInvoiceId == groupedInvoiceId).ToList();
        return Task.FromResult(lines);
    }
}
