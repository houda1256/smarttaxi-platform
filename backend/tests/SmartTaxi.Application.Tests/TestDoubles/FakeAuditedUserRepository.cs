using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Identity.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeAuditedUserRepository : IAuditedUserRepository
{
    private readonly List<AuditLogEntry> _auditEntries = new();

    public IReadOnlyList<AuditLogEntry> AuditEntries => _auditEntries;

    public Task SaveWithAuditAsync(User user, AuditLogEntry auditEntry, CancellationToken cancellationToken)
    {
        _auditEntries.Add(auditEntry);
        return Task.CompletedTask;
    }
}
