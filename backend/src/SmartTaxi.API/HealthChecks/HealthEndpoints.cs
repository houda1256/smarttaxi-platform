using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartTaxi.API.HealthChecks;

/// <summary>
/// Liveness proves process responsiveness only (Predicate never runs the
/// Postgres check). Readiness runs only checks tagged "ready" — today just
/// Postgres. Both are unauthenticated by design (future orchestrator/load
/// balancer probes) and explicitly bypass rate limiting so a healthy
/// instance is never killed for being probed too often. Response body is
/// deliberately minimal — no connection string, hostname, exception detail,
/// or provider diagnostics.
/// </summary>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = WriteStatusAsync
            })
            .WithTags("Health")
            .DisableRateLimiting();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready"),
                ResponseWriter = WriteStatusAsync
            })
            .WithTags("Health")
            .DisableRateLimiting();

        return app;
    }

    private static Task WriteStatusAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        var status = report.Status == HealthStatus.Healthy ? "Healthy" : "Unhealthy";
        return context.Response.WriteAsync($$"""{"status":"{{status}}"}""");
    }
}
