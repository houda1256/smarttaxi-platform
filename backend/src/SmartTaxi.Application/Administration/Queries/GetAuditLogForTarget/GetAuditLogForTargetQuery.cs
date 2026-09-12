using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Administration.Entities;
using SmartTaxi.Domain.Administration.Enums;

namespace SmartTaxi.Application.Administration.Queries.GetAuditLogForTarget;

public sealed record GetAuditLogForTargetQuery(AuditTargetType TargetType, Guid TargetId, int PageNumber, int PageSize)
    : IQuery<PagedResult<AuditLogEntry>>;
