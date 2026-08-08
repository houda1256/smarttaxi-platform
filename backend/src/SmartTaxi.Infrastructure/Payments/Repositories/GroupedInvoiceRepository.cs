using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class GroupedInvoiceRepository : IGroupedInvoiceRepository
{
    private readonly ApplicationDbContext _context;

    public GroupedInvoiceRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(GroupedInvoice invoice, CancellationToken cancellationToken)
    {
        await _context.GroupedInvoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<GroupedInvoice?> GetByIdAsync(Guid groupedInvoiceId, CancellationToken cancellationToken) =>
        _context.GroupedInvoices.FirstOrDefaultAsync(invoice => invoice.Id == groupedInvoiceId, cancellationToken);

    public async Task<PagedResult<GroupedInvoice>> GetForBusinessCustomerAsync(
        Guid businessCustomerId, GroupedInvoiceStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.GroupedInvoices.Where(invoice => invoice.BusinessCustomerId == businessCustomerId);

        if (status is not null)
        {
            query = query.Where(invoice => invoice.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(invoice => invoice.IssueDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<GroupedInvoice>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<GroupedInvoice>> GetOverdueAsync(DateTime asOfUtc, CancellationToken cancellationToken) =>
        await _context.GroupedInvoices
            .Where(invoice => invoice.Status == GroupedInvoiceStatus.Issued && invoice.DueDate < asOfUtc)
            .OrderBy(invoice => invoice.DueDate)
            .ToListAsync(cancellationToken);

    public async Task<bool> TryMarkPaidAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.GroupedInvoices
            .Where(invoice => invoice.Id == groupedInvoiceId && invoice.Status == GroupedInvoiceStatus.Issued)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invoice => invoice.Status, GroupedInvoiceStatus.Paid), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryCancelAsync(Guid groupedInvoiceId, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.GroupedInvoices
            .Where(invoice => invoice.Id == groupedInvoiceId && invoice.Status == GroupedInvoiceStatus.Issued)
            .ExecuteUpdateAsync(setters => setters.SetProperty(invoice => invoice.Status, GroupedInvoiceStatus.Cancelled), cancellationToken);

        return rows == 1;
    }
}
