namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class CorsTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public CorsTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EmptyConfiguredOrigins_NeverGrantsAnArbitraryOrigin()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://random-site.example.com");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task ConfiguredOrigin_ReceivesAccessControlAllowOriginHeader()
    {
        var overrides = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://allowed.example.com" };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://allowed.example.com");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal("https://allowed.example.com", Assert.Single(values));
    }

    [Fact]
    public async Task DisallowedOrigin_NeverReceivesAccessControlAllowOriginHeader()
    {
        var overrides = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://allowed.example.com" };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("Origin", "https://not-allowed.example.com");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_ForConfiguredOrigin_ReturnsCorsHeaders()
    {
        var overrides = new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "https://allowed.example.com" };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        request.Headers.Add("Origin", "https://allowed.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
