using System.Threading.RateLimiting;

namespace SmartTaxi.API.RateLimiting;

public static class RateLimitingExtensions
{
    /// <summary>
    /// One global limiter covering the whole API via RateLimitPolicySelector
    /// — health and hub endpoints opt out individually via
    /// .DisableRateLimiting() at their own mapping, which bypasses this
    /// entirely regardless of path/method.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, cancellationToken) => RateLimitRejectionWriter.WriteAsync(context, cancellationToken);

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(RateLimitPolicySelector.SelectPartition);
        });

        return services;
    }
}
