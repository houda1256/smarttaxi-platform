using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.API.HealthChecks;

/// <summary>
/// Readiness-only — PostgreSQL is the sole mandatory runtime dependency in
/// this backend (every repository/service goes through ApplicationDbContext;
/// email/SMS/push/file-storage/report-export are all in-process dev stubs
/// today, never a real external dependency). Uses the framework's own
/// Database.CanConnectAsync — no extra NuGet package (not AddDbContextCheck,
/// not a Npgsql-specific health-check package). Never surfaces the
/// connection string, hostname, or exception details.
/// </summary>
public sealed class PostgresHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public PostgresHealthCheck(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
        return canConnect ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy();
    }
}
