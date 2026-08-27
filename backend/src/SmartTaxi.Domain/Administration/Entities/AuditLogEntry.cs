using System.Text.Json;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Administration.Entities;

/// <summary>
/// Append-only, cross-cutting — deliberately its own bounded context, not
/// bolted onto Identity, since it already audits both Identity actions
/// (suspend/reactivate/session-revoke/2FA-reset/role changes) and will
/// naturally extend to other modules later. No update/delete method exists
/// anywhere on this type or its repository — the enforcement chain is
/// application-level (no mutator), EF-level (private set everywhere), and a
/// PostgreSQL trigger (see AuditLogEntryConfiguration/the migration) — never
/// claimed as protection against a database superuser.
///
/// ActorUserId is nullable: most events have a human actor, but
/// AccountLocked is system-triggered by the failed-login mechanism itself,
/// with no admin behind it — forcing a non-null actor would mean fabricating
/// one.
///
/// Metadata is never a generic reflection-based entity diff/snapshot. Every
/// caller passes a small, explicit, hand-picked key-value set (0-2 pairs per
/// the approved catalog); this factory only serializes and bounds it, it
/// never inspects an entity to decide what belongs in it.
///
/// CorrelationId/IpAddress/UserAgent are deliberately domain-model-agnostic
/// of HttpContext — this entity has no dependency on ASP.NET Core at all.
/// They are supplied as plain nullable strings by whichever caller has them
/// (via IAuditContextAccessor, an Application-layer abstraction) and a
/// non-HTTP caller can simply pass null for all three.
/// </summary>
public sealed class AuditLogEntry : Entity
{
    private const int MaxCorrelationIdLength = 64;
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 500;
    private const int MaxMetadataLength = 2000;

    public Guid? ActorUserId { get; private set; }
    public AuditAction Action { get; private set; }
    public AuditTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? Metadata { get; private set; }

    private AuditLogEntry()
    {
    }

    private AuditLogEntry(
        Guid? actorUserId, AuditAction action, AuditTargetType targetType, Guid targetId, string? metadata, string? correlationId,
        string? ipAddress, string? userAgent, DateTime utcNow)
        : base(Guid.NewGuid())
    {
        ActorUserId = actorUserId;
        Action = action;
        TargetType = targetType;
        TargetId = targetId;
        OccurredAtUtc = utcNow;
        Metadata = metadata;
        CorrelationId = Truncate(correlationId, MaxCorrelationIdLength);
        IpAddress = Truncate(ipAddress, MaxIpAddressLength);
        UserAgent = Truncate(userAgent, MaxUserAgentLength);
    }

    public static AuditLogEntry Create(
        Guid? actorUserId, AuditAction action, AuditTargetType targetType, Guid targetId,
        IReadOnlyDictionary<string, string>? metadata, string? correlationId, string? ipAddress, string? userAgent, DateTime utcNow)
    {
        if (targetId == Guid.Empty)
        {
            throw new ArgumentException("La cible de l'entrée d'audit est requise.");
        }

        var serializedMetadata = SerializeMetadata(metadata);

        return new AuditLogEntry(actorUserId, action, targetType, targetId, serializedMetadata, correlationId, ipAddress, userAgent, utcNow);
    }

    private static string? SerializeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        var serialized = JsonSerializer.Serialize(metadata);

        if (serialized.Length > MaxMetadataLength)
        {
            // Every approved event passes at most 2 short key-value pairs — reaching this length is a
            // programming error (someone violating the allow-list convention), never attacker-controlled
            // input, so failing loudly here is correct rather than silently truncating audit data.
            throw new ArgumentException("Les métadonnées d'audit dépassent la longueur maximale autorisée.");
        }

        return serialized;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
