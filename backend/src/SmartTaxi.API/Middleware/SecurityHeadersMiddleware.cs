namespace SmartTaxi.API.Middleware;

/// <summary>
/// Headers genuinely useful for this backend (a pure JSON API with a handful
/// of authenticated file-stream downloads and no server-rendered HTML) —
/// deliberately NOT a blanket copy of browser-web-app headers. No CSP/
/// Permissions-Policy: those govern browser rendering/feature access this
/// API never provides. Cache-Control: no-store is global and correct here —
/// the repository audit found no publicly cacheable resource anywhere (every
/// file download is per-user authorization-gated, and the one public route
/// returns time-sensitive token data).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        // /openapi and /scalar only exist at all behind the Development gate in
        // Program.cs, so excluding them here changes nothing in Production —
        // it only avoids X-Frame-Options/no-store fighting with Scalar's own
        // asset loading/caching while browsing the dev-only API reference UI.
        var isDevToolingRoute = context.Request.Path.StartsWithSegments("/openapi")
            || context.Request.Path.StartsWithSegments("/scalar");

        if (isDevToolingRoute)
        {
            return _next(context);
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Cache-Control"] = "no-store";
            return Task.CompletedTask;
        });

        return _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.UseMiddleware<SecurityHeadersMiddleware>();
}
