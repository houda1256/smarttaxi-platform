using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAuditLogRepository : IAuditLogRepository
{
    private readonly List<AuditLogEntry> _entries = new();

    public IReadOnlyList<AuditLogEntry> Entries => _entries;

    public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<PagedResult<AuditLogEntry>> GetForTargetAsync(
        AuditTargetType targetType, Guid targetId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var matching = _entries
            .Where(e => e.TargetType == targetType && e.TargetId == targetId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToList();

        var page = matching
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult(new PagedResult<AuditLogEntry>(page, matching.Count, pageNumber, pageSize));
    }
}
