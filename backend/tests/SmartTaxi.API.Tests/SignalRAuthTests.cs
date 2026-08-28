using System.Net;

namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class SignalRAuthTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public SignalRAuthTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RideHubNegotiate_WithoutAnyToken_ReturnsUnauthorized()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/hubs/rides/negotiate?negotiateVersion=1", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RideHubNegotiate_WithBearerHeader_StillWorks()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());

        var response = await client.PostAsync("/hubs/rides/negotiate?negotiateVersion=1", content: null);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task RideHubNegotiate_WithAccessTokenQueryParameter_IsAccepted()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = TestJwt.CreateToken();

        var response = await client.PostAsync($"/hubs/rides/negotiate?negotiateVersion=1&access_token={token}", content: null);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task NotificationHubNegotiate_WithAccessTokenQueryParameter_IsAccepted()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = TestJwt.CreateToken();

        var response = await client.PostAsync($"/hubs/notifications/negotiate?negotiateVersion=1&access_token={token}", content: null);

        Assert.True(response.IsSuccessStatusCode);
    }

    [Fact]
    public async Task OrdinaryApiRoute_WithAccessTokenQueryParameterOnly_IsStillUnauthorized()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        var token = TestJwt.CreateToken();

        // The access_token query-string fallback is scoped to /hubs/** only —
        // an ordinary API route must never accept it as an alternative to the
        // Authorization header.
        var response = await client.GetAsync($"/api/users/me?access_token={token}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
