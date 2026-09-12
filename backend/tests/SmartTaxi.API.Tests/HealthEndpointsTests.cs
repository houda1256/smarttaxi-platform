using System.Net;

namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class HealthEndpointsTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public HealthEndpointsTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Live_ReturnsHealthyWithoutTouchingTheDatabase()
    {
        // A deliberately-bad connection string proves liveness never queries Postgres.
        await using var factory = new SmartTaxiApiFactory(
            "Host=127.0.0.1;Port=1;Database=nonexistent;Username=x;Password=x;Timeout=1");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"status\":\"Healthy\"}", body);
    }

    [Fact]
    public async Task Ready_ReturnsHealthyWhenPostgresIsReachable()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"status\":\"Healthy\"}", body);
    }

    [Fact]
    public async Task Ready_ReturnsUnhealthyAndNoSensitiveDetailsWhenPostgresIsUnavailable()
    {
        await using var factory = new SmartTaxiApiFactory(
            "Host=127.0.0.1;Port=1;Database=nonexistent;Username=x;Password=x;Timeout=1");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("{\"status\":\"Unhealthy\"}", body);

        Assert.DoesNotContain("nonexistent", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Health_BypassesRateLimiting_ManyRequestsAllSucceed()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:AuthenticatedGeneral:PermitLimit"] = "1",
            ["RateLimiting:AuthenticatedGeneral:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();

        for (var i = 0; i < 10; i++)
        {
            var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
