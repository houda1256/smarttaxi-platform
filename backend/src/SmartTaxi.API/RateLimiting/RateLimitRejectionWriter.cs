using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SmartTaxi.API.Correlation;

namespace SmartTaxi.API.RateLimiting;

/// <summary>
/// Never includes the partition key, IP, UserId, or internal limiter
/// configuration — only what the client needs to know: it was rejected, why
/// in general terms, when it may retry (only if the algorithm actually
/// provides that metadata — never fabricated), and the request's correlation
/// id for support purposes.
/// </summary>
internal static class RateLimitRejectionWriter
{
    public static async ValueTask WriteAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        var problemDetails = new ProblemDetails
        {
            Title = "Trop de requêtes.",
            Detail = "La limite de requêtes a été atteinte. Réessayez plus tard.",
            Status = StatusCodes.Status429TooManyRequests
        };

        var correlationId = httpContext.GetCorrelationId();

        if (correlationId is not null)
        {
            problemDetails.Extensions["correlationId"] = correlationId;
        }

        // WriteAsJsonAsync ignores any Response.ContentType set beforehand
        // unless the content type is passed explicitly here — a manual
        // assignment above this call would silently be overwritten with
        // "application/json".
        await httpContext.Response.WriteAsJsonAsync(
            problemDetails, options: null, contentType: "application/problem+json", cancellationToken);
    }
}
