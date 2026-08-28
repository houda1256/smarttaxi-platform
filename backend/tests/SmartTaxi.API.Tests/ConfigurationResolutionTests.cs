using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SmartTaxi.Infrastructure.Configuration;

namespace SmartTaxi.API.Tests;

/// <summary>
/// Proves RateLimitingOptions overrides reach the app via DI — the
/// mechanism RateLimitPolicySelector and ConfiguredCorsPolicyProvider both
/// rely on precisely because an eager pre-Build() configuration read (as
/// Program.cs uses for JwtOptions, matching an existing pre-Module-13B
/// pattern) does not reliably see a WebApplicationFactory test override.
/// </summary>
[Collection("SharedApiPostgres")]
public class ConfigurationResolutionTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public ConfigurationResolutionTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void RateLimitingOverride_IsVisibleViaDIOptions()
    {
        var overrides = new Dictionary<string, string?> { ["RateLimiting:AuthCritical:PermitLimit"] = "2" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        var options = factory.Services.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

        Assert.Equal(2, options.AuthCritical.PermitLimit);
    }
}
