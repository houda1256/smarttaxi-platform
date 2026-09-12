using Microsoft.AspNetCore.Cors.Infrastructure;

namespace SmartTaxi.API.Cors;

/// <summary>
/// Always registers CORS — ConfiguredCorsPolicyProvider decides per request
/// whether a policy applies, based on the current CorsOptions. Empty
/// AllowedOrigins (the default in every environment today — no frontend
/// origin is authoritative yet) means every request gets no policy at all,
/// which is CORS-disabled behavior. Never AllowAnyOrigin.
/// </summary>
public static class CorsExtensions
{
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddCors();
        services.AddSingleton<ICorsPolicyProvider, ConfiguredCorsPolicyProvider>();
        return services;
    }
}
