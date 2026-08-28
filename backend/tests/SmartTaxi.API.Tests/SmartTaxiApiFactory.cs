using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace SmartTaxi.API.Tests;

/// <summary>
/// Wires a real Postgres connection plus the minimal test-only config (JWT
/// key) every test needs; per-test config overrides (small rate limits) are
/// layered on top.
///
/// KNOWN LIMITATION: ConfigureAppConfiguration overrides are only proven
/// reliable when environment stays at its "Development" default. Under
/// WebApplicationFactory + this project's minimal-hosting Program.cs, a
/// non-Development environment value (verified with both "Production" and
/// "Staging", and with the real ASPNETCORE_ENVIRONMENT process variable set
/// directly) causes this factory's overrides — including the connection
/// string — to stop reaching Program.cs's early configuration reads, for a
/// reason not fully root-caused within this pass's time budget. Tests that
/// need a non-Development environment (HSTS presence) are therefore not
/// covered by an automated WebApplicationFactory test; see
/// SecurityHeadersAndHstsTests' comment for how that gap was verified
/// instead.
/// </summary>
public sealed class SmartTaxiApiFactory : WebApplicationFactory<Program>
{
    public const string TestJwtKey = "test-only-signing-key-at-least-32-characters-long-0123456789";

    private readonly string _connectionString;
    private readonly IReadOnlyDictionary<string, string?> _overrides;
    private readonly string _environment;

    public SmartTaxiApiFactory(
        string connectionString, IReadOnlyDictionary<string, string?>? overrides = null, string environment = "Development")
    {
        _connectionString = connectionString;
        _overrides = overrides ?? new Dictionary<string, string?>();
        _environment = environment;

        // Environment variables are one of WebApplicationBuilder.CreateBuilder(args)'s
        // own default configuration sources, read during its own early
        // initialization — unlike a ConfigureAppConfiguration override
        // (added later, too late for Program.cs's eager pre-Build() reads;
        // see this class's own doc comment), this reliably reaches them.
        // Both overrides are required: without them, Development environment
        // silently falls back to this machine's real local dotnet
        // user-secrets (a real Postgres database and a real Jwt:Key) instead
        // of this factory's isolated Testcontainer and test-only signing key
        // — confirmed by finding real seeded/mutated rows in that local
        // database from early runs before this fix, since cleaned up.
        Environment.SetEnvironmentVariable("Jwt__Key", TestJwtKey);
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", _connectionString);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            // Defaults first, then _overrides layered on top — so a test that
            // deliberately wants an invalid value (e.g. a bad Jwt:Key, to prove
            // startup validation fails) can actually override the default,
            // rather than being silently overwritten by it.
            // Jwt:Issuer/Jwt:Audience are deliberately NOT overridden here —
            // Program.cs's AddJwtBearer configuration reads them from an
            // eager, pre-Build() configuration snapshot that a
            // ConfigureAppConfiguration override added by
            // WebApplicationFactory does not reach, so any override here
            // would silently have no effect on real token validation while
            // still appearing to "work" via DI-resolved IOptions<JwtOptions>
            // elsewhere — a confusing split. TestJwt.CreateToken() issues
            // tokens using appsettings.json's actual values ("SmartTaxi")
            // instead. Jwt:Key has no such conflict (appsettings.json sets
            // no key at all), so it's safe to override here.
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                ["Jwt:Key"] = TestJwtKey
            };

            foreach (var (key, value) in _overrides)
            {
                settings[key] = value;
            }

            config.AddInMemoryCollection(settings);
        });

        // TestServer has no real socket, so Connection.RemoteIpAddress is
        // always null — unlike Kestrel in production, which always
        // populates it. This filter runs first in the pipeline (ahead of
        // every app.Use call in Program.cs) and stands in for that missing
        // transport-layer behavior, purely for test observability; it has
        // no production equivalent and is not related to (nor a substitute
        // for) the deferred trusted-forwarded-header work.
        builder.ConfigureTestServices(services =>
            services.AddSingleton<IStartupFilter>(new SyntheticRemoteIpAddressStartupFilter()));
    }

    private sealed class SyntheticRemoteIpAddressStartupFilter : IStartupFilter
    {
        private static readonly IPAddress SyntheticClientIp = IPAddress.Parse("203.0.113.42");

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = SyntheticClientIp;
                await nextMiddleware();
            });
            next(app);
        };
    }
}
