using System.Net;

namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class CleanupVerificationTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public CleanupVerificationTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task VulnerableSqlDemoEndpoint_NoLongerExists()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/security-demo/vulnerable-sql?search=x");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WeatherForecastEndpoint_NoLongerExists()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/weatherforecast");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
