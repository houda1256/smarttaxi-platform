using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeDocumentAccessAuditRepository : IDocumentAccessAuditRepository
{
    private readonly List<DocumentAccessAuditEntry> _entries = [];

    public Task AddAsync(DocumentAccessAuditEntry entry, CancellationToken cancellationToken)
    {
        _entries.Add(entry);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<DocumentAccessAuditEntry> Entries => _entries;
}
