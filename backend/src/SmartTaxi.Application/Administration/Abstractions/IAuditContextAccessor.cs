namespace SmartTaxi.Application.Administration.Abstractions;

/// <summary>
/// The only seam through which an HTTP-derived correlation id/IP/user-agent
/// ever reaches an audit entry. AuditLogEntry itself has no dependency on
/// HttpContext at all — a non-HTTP caller (a background process, a future
/// scheduled job) can construct a perfectly valid entry with all three
/// fields null. In this pass (Module 13A), the Infrastructure implementation
/// always returns an all-null context — the real HTTP correlation
/// middleware is explicitly Module 13B scope, not built here.
/// </summary>
public interface IAuditContextAccessor
{
    AuditContextInfo GetContext();
}

public sealed record AuditContextInfo(string? CorrelationId, string? IpAddress, string? UserAgent)
{
    public static readonly AuditContextInfo None = new(null, null, null);
}
