using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeGroupedInvoiceRepository : IGroupedInvoiceRepository
{
    private readonly Dictionary<Guid, GroupedInvoice> _invoicesById = new();

    public Task AddAsync(GroupedInvoice invoice, CancellationToken cancellationToken)
    {
        _invoicesById[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task<GroupedInvoice?> GetByIdAsync(Guid groupedInvoiceId, CancellationToken cancellationToken) =>
        Task.FromResult(_invoicesById.GetValueOrDefault(groupedInvoiceId));

    public Task<PagedResult<GroupedInvoice>> GetForBusinessCustomerAsync(
        Guid businessCustomerId, GroupedInvoiceStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _invoicesById.Values.Where(i => i.BusinessCustomerId == businessCustomerId);

        if (status is not null)
        {
            query = query.Where(i => i.Status == status);
        }

        var items = query.ToList();
        return Task.FromResult(new PagedResult<GroupedInvoice>(items, items.Count, pageNumber, pageSize));
    }

    public Task<IReadOnlyCollection<GroupedInvoice>> GetOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<GroupedInvoice> overdue = _invoicesById.Values
            .Where(i => i.Status == GroupedInvoiceStatus.Issued && i.DueDate < asOfUtc)
            .ToList();
        return Task.FromResult(overdue);
    }

    public Task<bool> TryMarkPaidAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(groupedInvoiceId, GroupedInvoiceStatus.Issued, GroupedInvoiceStatus.Paid);

    public Task<bool> TryCancelAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken) =>
        TryTransition(groupedInvoiceId, GroupedInvoiceStatus.Issued, GroupedInvoiceStatus.Cancelled);

    private Task<bool> TryTransition(Guid groupedInvoiceId, GroupedInvoiceStatus from, GroupedInvoiceStatus to)
    {
        if (!_invoicesById.TryGetValue(groupedInvoiceId, out var invoice) || invoice.Status != from)
        {
            return Task.FromResult(false);
        }

        typeof(GroupedInvoice).GetProperty(nameof(GroupedInvoice.Status))!.SetValue(invoice, to);
        return Task.FromResult(true);
    }
}
