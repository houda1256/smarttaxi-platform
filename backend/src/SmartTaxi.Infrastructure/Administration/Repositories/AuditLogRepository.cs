using Microsoft.EntityFrameworkCore;
using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Administration.Repositories;

internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        await _context.AuditLogs.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogEntry>> GetForTargetAsync(
        AuditTargetType targetType, Guid targetId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.AuditLogs
            .Where(entry => entry.TargetType == targetType && entry.TargetId == targetId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogEntry>(items, totalCount, pageNumber, pageSize);
    }
}
