using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.Documents.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Identity.Repositories;

internal sealed class DocumentAccessAuditRepository : IDocumentAccessAuditRepository
{
    private readonly ApplicationDbContext _context;

    public DocumentAccessAuditRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DocumentAccessAuditEntry entry, CancellationToken cancellationToken)
    {
        await _context.DocumentAccessAuditEntries.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
