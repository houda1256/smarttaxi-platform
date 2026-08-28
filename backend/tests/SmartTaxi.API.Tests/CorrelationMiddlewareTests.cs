namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class CorrelationMiddlewareTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public CorrelationMiddlewareTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NoIncomingHeader_GeneratesASafeId()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        var generated = Assert.Single(values);
        Assert.False(string.IsNullOrWhiteSpace(generated));
        Assert.True(generated.Length <= 64);
    }

    [Fact]
    public async Task ValidIncomingHeader_IsPreservedAndEchoedBack()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "client-supplied-id-123");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal("client-supplied-id-123", Assert.Single(values));
    }

    [Fact]
    public async Task InvalidCharacters_AreIgnoredAndAnIdIsGeneratedInstead()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        // Includes characters outside [A-Za-z0-9-] and a CRLF-injection attempt.
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "bad id\r\nX-Injected: evil");

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        var echoed = Assert.Single(values);
        Assert.DoesNotContain("bad id", echoed);
        Assert.DoesNotContain("Injected", echoed);
        Assert.False(response.Headers.Contains("X-Injected"));
    }

    [Fact]
    public async Task TooLongHeader_IsIgnoredAndAnIdIsGeneratedInstead()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        var tooLong = new string('a', 65);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", tooLong);

        var response = await client.SendAsync(request);

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        var echoed = Assert.Single(values);
        Assert.NotEqual(tooLong, echoed);
        Assert.True(echoed.Length <= 64);
    }
}
