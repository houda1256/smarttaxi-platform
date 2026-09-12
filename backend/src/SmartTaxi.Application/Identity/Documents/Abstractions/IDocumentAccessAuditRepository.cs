using SmartTaxi.Domain.Identity.Documents.Entities;

namespace SmartTaxi.Application.Identity.Documents.Abstractions;

public interface IDocumentAccessAuditRepository
{
    Task AddAsync(DocumentAccessAuditEntry entry, CancellationToken cancellationToken);
}
