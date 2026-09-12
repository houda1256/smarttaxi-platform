using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Configuration;

namespace SmartTaxi.API.RateLimiting;

/// <summary>
/// One central, path/method-based partition selector — deliberately not
/// hundreds of per-endpoint .RequireRateLimiting() calls. Route tables below
/// are the exhaustive, source-verified list from the Module 13B route audit;
/// nothing here was invented. Evaluated top-to-bottom: the small, exact
/// unauthenticated-abuse-risk sets first, then the broad /api/admin mutation
/// prefix, then the specific payment/finance/upload prefixes, then the
/// authenticated-general fallback for everything else.
///
/// Options are resolved per-request from HttpContext.RequestServices rather
/// than captured once at startup — PartitionedRateLimiter.Create's
/// partitioner runs on every request anyway, and per-request DI resolution
/// is the same mechanism every other option-driven decision in this app
/// already relies on (avoids depending on a pre-Build() configuration
/// snapshot, which is not guaranteed to reflect every configuration source
/// by the time Program.cs's top-level code runs).
/// </summary>
internal static class RateLimitPolicySelector
{
    private static readonly (string Method, string Path)[] AuthCriticalRoutes =
    [
        ("POST", "/api/auth/register"),
        ("POST", "/api/auth/login"),
        ("POST", "/api/auth/refresh"),
        ("POST", "/api/auth/password/forgot"),
        ("POST", "/api/auth/password/reset"),
        ("POST", "/api/auth/email-verification/request"),
        ("POST", "/api/auth/email-verification/confirm")
    ];

    private static readonly (string Method, string Path)[] OtpRoutes =
    [
        ("POST", "/api/auth/2fa/challenge"),
        ("POST", "/api/auth/2fa/confirm"),
        ("POST", "/api/auth/phone-verification/request"),
        ("POST", "/api/auth/phone-verification/confirm")
    ];

    private static readonly string[] MutationMethods = ["POST", "PUT", "PATCH", "DELETE"];

    private static readonly (string PathPrefix, string[] Methods)[] SensitiveMutationRoutes =
    [
        ("/api/payments", ["POST"]),
        ("/api/finance/payouts", ["POST"]),
        ("/api/finance/cash-declarations", ["POST"]),
        ("/api/finance/cash-register", ["POST"]),
        ("/api/finance/disputes", ["POST"]),
        ("/api/users/me/documents", ["POST"]),
        ("/api/fleet/vehicles", ["POST"]),
        ("/api/advertising/campaigns", ["POST"])
    ];

    public static RateLimitPartition<string> SelectPartition(HttpContext context)
    {
        var options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        if (MatchesExact(path, method, AuthCriticalRoutes))
        {
            return FixedWindow($"auth-critical:{IpKey(context)}", options.AuthCritical);
        }

        if (MatchesExact(path, method, OtpRoutes))
        {
            return FixedWindow($"otp:{IdentityKey(context)}", options.OtpAndChallenge);
        }

        if (method == "GET" && path.StartsWith("/api/public/ride-share", StringComparison.OrdinalIgnoreCase))
        {
            return FixedWindow($"public-read:{IpKey(context)}", options.PublicRead);
        }

        if (path.StartsWith("/api/admin", StringComparison.OrdinalIgnoreCase) && MutationMethods.Contains(method))
        {
            return SlidingWindow($"admin-mutation:{IdentityKey(context)}", options.AdminMutation);
        }

        if (MatchesPrefix(path, method, SensitiveMutationRoutes))
        {
            return TokenBucket($"sensitive-mutation:{IdentityKey(context)}", options.SensitiveMutation);
        }

        return SlidingWindow($"general:{IdentityKey(context)}", options.AuthenticatedGeneral);
    }

    private static bool MatchesExact(string path, string method, (string Method, string Path)[] routes) =>
        routes.Any(route => route.Method == method && string.Equals(route.Path, path, StringComparison.OrdinalIgnoreCase));

    private static bool MatchesPrefix(string path, string method, (string PathPrefix, string[] Methods)[] routes) =>
        routes.Any(route =>
            path.StartsWith(route.PathPrefix, StringComparison.OrdinalIgnoreCase) && route.Methods.Contains(method));

    /// <summary>Never trusts X-Forwarded-For — see the Module 13B design audit on proxy topology. A stable fallback, never a random per-request value, so a null RemoteIpAddress can never bypass the limit.</summary>
    private static string IpKey(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";

    /// <summary>Stable authenticated UserId (the "sub" claim) — never Role. Falls back to the IP key when unauthenticated, which is exactly what OtpAndChallenge's mixed authenticated/unauthenticated routes need.</summary>
    private static string IdentityKey(HttpContext context) =>
        context.User.FindFirst("sub")?.Value is { Length: > 0 } sub ? sub : IpKey(context);

    private static RateLimitPartition<string> FixedWindow(string key, FixedWindowPolicyOptions policy) =>
        RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = TimeSpan.FromSeconds(policy.WindowSeconds),
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    private static RateLimitPartition<string> SlidingWindow(string key, SlidingWindowPolicyOptions policy) =>
        RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = policy.PermitLimit,
            Window = TimeSpan.FromSeconds(policy.WindowSeconds),
            SegmentsPerWindow = policy.SegmentsPerWindow,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });

    private static RateLimitPartition<string> TokenBucket(string key, TokenBucketPolicyOptions policy) =>
        RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = policy.TokenLimit,
            TokensPerPeriod = policy.TokensPerPeriod,
            ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds),
            AutoReplenishment = policy.AutoReplenishment,
            QueueLimit = policy.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
}
