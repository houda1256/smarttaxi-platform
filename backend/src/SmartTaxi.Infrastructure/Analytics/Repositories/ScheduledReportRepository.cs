using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Analytics.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Analytics.Entities;
using SmartTaxi.Domain.Analytics.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Analytics.Repositories;

internal sealed class ScheduledReportRepository : IScheduledReportRepository
{
    private readonly ApplicationDbContext _context;

    public ScheduledReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryAddAsync(ScheduledReportDefinition definition, CancellationToken cancellationToken)
    {
        await _context.ScheduledReportDefinitions.AddAsync(definition, cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _context.Entry(definition).State = EntityState.Detached;
            return false;
        }
    }

    public Task<ScheduledReportDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _context.ScheduledReportDefinitions.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<PagedResult<ScheduledReportDefinition>> GetAllAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await _context.ScheduledReportDefinitions.CountAsync(cancellationToken);

        var items = await _context.ScheduledReportDefinitions
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ScheduledReportDefinition>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<Guid>> GetDueIdsAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var staleBefore = utcNow - IScheduledReportRepository.StaleClaimThreshold;

        return await _context.ScheduledReportDefinitions
            .Where(d => d.IsActive && d.NextRunAtUtc <= utcNow
                && (d.ProcessingClaimedAtUtc == null || d.ProcessingClaimedAtUtc < staleBefore))
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var staleBefore = utcNow - IScheduledReportRepository.StaleClaimThreshold;

        var rows = await _context.ScheduledReportDefinitions
            .Where(d => d.Id == id && d.IsActive && d.NextRunAtUtc <= utcNow
                && (d.ProcessingClaimedAtUtc == null || d.ProcessingClaimedAtUtc < staleBefore))
            .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.ProcessingClaimedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryFinalizeAsync(
        Guid id, DateTime claimedAtUtc, DateTime nextRunAtUtc, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.ScheduledReportDefinitions
            .Where(d => d.Id == id && d.ProcessingClaimedAtUtc == claimedAtUtc)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.ProcessingClaimedAtUtc, (DateTime?)null)
                .SetProperty(d => d.NextRunAtUtc, nextRunAtUtc)
                .SetProperty(d => d.LastProcessedAtUtc, utcNow)
                .SetProperty(d => d.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryUpdateAsync(
        Guid id, ScheduledReportCategory category, ScheduledReportFrequency frequency, Guid recipientUserId, DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var rows = await _context.ScheduledReportDefinitions
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.Category, category)
                .SetProperty(d => d.Frequency, frequency)
                .SetProperty(d => d.RecipientUserId, recipientUserId)
                .SetProperty(d => d.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }

    public async Task<bool> TryDeactivateAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken)
    {
        var rows = await _context.ScheduledReportDefinitions
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(d => d.IsActive, false)
                .SetProperty(d => d.UpdatedAtUtc, utcNow), cancellationToken);

        return rows == 1;
    }
}
