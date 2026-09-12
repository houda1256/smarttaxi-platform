using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Payments.GroupedInvoicing.Abstractions;
using SmartTaxi.Domain.Payments.GroupedInvoicing.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Payments.Repositories;

internal sealed class GroupedInvoiceLineRepository : IGroupedInvoiceLineRepository
{
    private readonly ApplicationDbContext _context;

    public GroupedInvoiceLineRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<bool> AnyAlreadyInvoicedAsync(IReadOnlyCollection<Guid> rideIds, CancellationToken cancellationToken) =>
        _context.GroupedInvoiceLines.AnyAsync(line => rideIds.Contains(line.RideId), cancellationToken);

    public async Task<bool> TryAddRangeAsync(IReadOnlyCollection<GroupedInvoiceLine> lines, CancellationToken cancellationToken)
    {
        await _context.GroupedInvoiceLines.AddRangeAsync(lines, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            // The unique index on RideId rejected a Ride that was already invoiced by a concurrent request.
            foreach (var line in lines)
            {
                _context.Entry(line).State = EntityState.Detached;
            }

            return false;
        }
    }

    public async Task<IReadOnlyCollection<GroupedInvoiceLine>> GetForInvoiceAsync(Guid groupedInvoiceId, CancellationToken cancellationToken) =>
        await _context.GroupedInvoiceLines
            .Where(line => line.GroupedInvoiceId == groupedInvoiceId)
            .OrderBy(line => line.CreatedAt)
            .ToListAsync(cancellationToken);
}
