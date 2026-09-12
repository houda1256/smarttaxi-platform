using SmartTaxi.API.Correlation;
using SmartTaxi.Application.Administration.Abstractions;

namespace SmartTaxi.API.Administration;

/// <summary>
/// API-layer implementation of the Application-layer IAuditContextAccessor —
/// the only place in this codebase that connects the two. AuditLogEntry and
/// every Application/Infrastructure caller of this interface remain entirely
/// HttpContext-free; only this class touches IHttpContextAccessor. Registered
/// in Program.cs, overriding Infrastructure's NullAuditContextAccessor for
/// normal request handling — background/non-HTTP callers still resolve the
/// null-object fallback (or, when there is genuinely no HttpContext even
/// though this accessor is active — e.g. a request-scoped background
/// continuation — GetContext() below degrades to AuditContextInfo.None
/// itself, the same value NullAuditContextAccessor always returns).
/// AuditLogEntry.Create already truncates every field to the schema's exact
/// limits, but this accessor also bounds them explicitly rather than relying
/// solely on that downstream safety net.
/// </summary>
public sealed class HttpCorrelationAuditContextAccessor : IAuditContextAccessor
{
    private const int MaxCorrelationIdLength = 64;
    private const int MaxIpAddressLength = 45;
    private const int MaxUserAgentLength = 500;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationAuditContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditContextInfo GetContext()
    {
        var httpContext = _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return AuditContextInfo.None;
        }

        var correlationId = Truncate(httpContext.GetCorrelationId(), MaxCorrelationIdLength);
        var ipAddress = Truncate(httpContext.Connection.RemoteIpAddress?.ToString(), MaxIpAddressLength);
        var userAgent = Truncate(httpContext.Request.Headers.UserAgent.ToString(), MaxUserAgentLength);

        return new AuditContextInfo(correlationId, ipAddress, userAgent);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length > maxLength ? value[..maxLength] : value;
    }
}
