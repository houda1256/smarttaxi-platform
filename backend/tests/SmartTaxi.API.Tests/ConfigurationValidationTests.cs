namespace SmartTaxi.API.Tests;

/// <summary>Each invalid config test triggers real host startup (accessing Services starts the TestServer, which runs every ValidateOnStart() check) and asserts it throws — never that a specific secret value leaks into the exception message.</summary>
[Collection("SharedApiPostgres")]
public class ConfigurationValidationTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public ConfigurationValidationTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void WeakJwtKey_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["Jwt:Key"] = "too-short-key" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        var exception = Assert.ThrowsAny<Exception>(() => _ = factory.Services);
        Assert.DoesNotContain("too-short-key", exception.ToString());
    }

    [Fact]
    public void MissingJwtIssuer_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["Jwt:Issuer"] = "" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        Assert.ThrowsAny<Exception>(() => _ = factory.Services);
    }

    [Fact]
    public void InvalidLoginLockoutMaxFailedAttempts_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["LoginLockout:MaxFailedAttempts"] = "0" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        Assert.ThrowsAny<Exception>(() => _ = factory.Services);
    }

    [Fact]
    public void InvalidRateLimitingPermitLimit_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["RateLimiting:AuthCritical:PermitLimit"] = "-1" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        Assert.ThrowsAny<Exception>(() => _ = factory.Services);
    }

    [Fact]
    public void InvalidCorsOrigin_WithWildcard_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://*.example.com" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        Assert.ThrowsAny<Exception>(() => _ = factory.Services);
    }

    [Fact]
    public void InvalidCorsOrigin_WithPath_FailsStartup()
    {
        var overrides = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://allowed.example.com/some-path" };
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        Assert.ThrowsAny<Exception>(() => _ = factory.Services);
    }

    [Fact]
    public void ValidConfiguration_StartsSuccessfully()
    {
        using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);

        var exception = Record.Exception(() => _ = factory.Services);

        Assert.Null(exception);
    }
}
