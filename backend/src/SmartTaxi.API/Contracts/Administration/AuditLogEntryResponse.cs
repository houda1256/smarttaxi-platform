using SmartTaxi.Domain.Administration.Entities;

namespace SmartTaxi.API.Contracts.Administration;

public sealed record AuditLogEntryResponse(
    Guid Id, Guid? ActorUserId, string Action, string TargetType, Guid TargetId, DateTime OccurredAtUtc,
    string? CorrelationId, string? IpAddress, string? UserAgent, string? Metadata)
{
    public static AuditLogEntryResponse FromEntity(AuditLogEntry entry) => new(
        entry.Id, entry.ActorUserId, entry.Action.ToString(), entry.TargetType.ToString(), entry.TargetId, entry.OccurredAtUtc,
        entry.CorrelationId, entry.IpAddress, entry.UserAgent, entry.Metadata);
}
