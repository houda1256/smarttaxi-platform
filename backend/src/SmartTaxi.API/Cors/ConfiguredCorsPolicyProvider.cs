using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using AppCorsOptions = SmartTaxi.Infrastructure.Configuration.CorsOptions;

namespace SmartTaxi.API.Cors;

/// <summary>
/// Resolves CorsOptions per request rather than once at startup — the same
/// reasoning as RateLimitPolicySelector. Returning null (no policy) when
/// AllowedOrigins is empty is CORS middleware's own "not configured" signal:
/// no Access-Control-Allow-Origin header is ever added, exactly the disabled
/// behavior an empty WithOrigins() list would otherwise ambiguously imply.
/// </summary>
internal sealed class ConfiguredCorsPolicyProvider : ICorsPolicyProvider
{
    private readonly IOptions<AppCorsOptions> _options;

    public ConfiguredCorsPolicyProvider(IOptions<AppCorsOptions> options)
    {
        _options = options;
    }

    public Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        var allowedOrigins = _options.Value.AllowedOrigins;

        if (allowedOrigins.Count == 0)
        {
            return Task.FromResult<CorsPolicy?>(null);
        }

        var policy = new CorsPolicyBuilder(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .Build();

        return Task.FromResult<CorsPolicy?>(policy);
    }
}
