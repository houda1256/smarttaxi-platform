using SmartTaxi.Application.Administration.Abstractions;

namespace SmartTaxi.Infrastructure.Administration.Services;

/// <summary>
/// Deliberate no-op for this pass: CorrelationId/IpAddress/UserAgent stay null
/// until Module 13B builds the HTTP correlation middleware that would actually
/// populate them. AuditLogEntry never depends on HttpContext, and neither does
/// this accessor.
/// </summary>
internal sealed class NullAuditContextAccessor : IAuditContextAccessor
{
    public AuditContextInfo GetContext() => AuditContextInfo.None;
}
