using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.Infrastructure.Administration.Repositories;

internal sealed class AuditedUserRepository : IAuditedUserRepository
{
    private readonly ApplicationDbContext _context;

    public AuditedUserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveWithAuditAsync(User user, AuditLogEntry auditEntry, CancellationToken cancellationToken)
    {
        // The user instance is already tracked by this scoped DbContext (loaded
        // via IUserRepository.GetByIdAsync earlier in the same request) and
        // already mutated by the caller, so only SaveChanges is needed for it —
        // this call exists to make that save and the audit insert commit or
        // roll back together as a single unit.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
