using SmartTaxi.Application.Administration.Abstractions;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Administration.Entities;

namespace SmartTaxi.Application.Administration.Queries.GetAuditLogForTarget;

public sealed class GetAuditLogForTargetQueryHandler : IQueryHandler<GetAuditLogForTargetQuery, PagedResult<AuditLogEntry>>
{
    private readonly IAuditLogRepository _repository;

    public GetAuditLogForTargetQueryHandler(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<AuditLogEntry>> Handle(GetAuditLogForTargetQuery query, CancellationToken cancellationToken) =>
        _repository.GetForTargetAsync(query.TargetType, query.TargetId, query.PageNumber, query.PageSize, cancellationToken);
}
