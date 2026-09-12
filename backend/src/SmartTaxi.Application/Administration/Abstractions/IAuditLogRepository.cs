using SmartTaxi.Application.Common;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.Application.Administration.Abstractions;

/// <summary>Append-only by contract — no Update/Delete member exists here, and none should ever be added.</summary>
public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<AuditLogEntry>> GetForTargetAsync(
        AuditTargetType targetType, Guid targetId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}
