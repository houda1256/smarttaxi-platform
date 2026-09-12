using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Tests;

[Collection("SharedApiPostgres")]
public class RateLimitingTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public RateLimitingTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuthCritical_AppliesToLogin_NPlus1RequestReturns429WithCorrelationIdAndRetryAfter()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:AuthCritical:PermitLimit"] = "2",
            ["RateLimiting:AuthCritical:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new { Email = "nobody@example.com", Password = "wrong" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
        Assert.Equal("application/problem+json", last.Content.Headers.ContentType?.MediaType);
        Assert.True(last.Headers.Contains("Retry-After"));

        var problem = await last.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("correlationId", out var correlationId));
        Assert.False(string.IsNullOrWhiteSpace(correlationId.GetString()));

        // Never leak the partition key, IP, UserId, or limiter configuration.
        var body = await client.GetStringAsync("/health/live"); // sanity: factory still responsive
        Assert.NotNull(body);
        Assert.DoesNotContain("PermitLimit", await last.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OtpAndChallenge_AppliesToTwoFactorChallenge_UnauthenticatedRequestsPartitionByIp()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:OtpAndChallenge:PermitLimit"] = "2",
            ["RateLimiting:OtpAndChallenge:WindowSeconds"] = "300"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/2fa/challenge", new { ChallengeToken = "not-real", Code = "000000" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task PublicRead_AppliesToPublicRideShare_NPlus1RequestReturns429()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:PublicRead:PermitLimit"] = "2",
            ["RateLimiting:PublicRead:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.GetAsync("/api/public/ride-share/not-a-real-token");
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task AdminMutation_AppliesToAdminSuspendRoute_NPlus1RequestReturns429()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:AdminMutation:PermitLimit"] = "2",
            ["RateLimiting:AdminMutation:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        // Rate limiting runs before authorization, so a token with no admin
        // permission still consumes a permit — it just also gets a 403.
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsync($"/api/admin/users/{Guid.NewGuid()}/suspend", content: null);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task SensitiveMutation_AppliesToPaymentCreation_NPlus1RequestReturns429()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:SensitiveMutation:TokenLimit"] = "2",
            ["RateLimiting:SensitiveMutation:TokensPerPeriod"] = "1",
            ["RateLimiting:SensitiveMutation:ReplenishmentPeriodSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: [Permissions.PaymentsCreate]));

        HttpResponseMessage? last = null;
        for (var i = 0; i < 3; i++)
        {
            last = await client.PostAsJsonAsync("/api/payments", new { RideId = Guid.NewGuid(), Method = "Cash" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last!.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedGeneral_PartitionsByUserId_OneUsersLimitDoesNotAffectAnother()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:AuthenticatedGeneral:PermitLimit"] = "1",
            ["RateLimiting:AuthenticatedGeneral:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);

        using var clientA = factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());
        using var clientB = factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());

        var firstForA = await clientA.GetAsync("/api/users/me");
        var secondForA = await clientA.GetAsync("/api/users/me");
        var firstForB = await clientB.GetAsync("/api/users/me");

        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstForA.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondForA.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, firstForB.StatusCode);
    }

    [Fact]
    public async Task Hubs_BypassRateLimiting_NegotiationNotRateLimitedEvenUnderTinyLimit()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:AuthenticatedGeneral:PermitLimit"] = "1",
            ["RateLimiting:AuthenticatedGeneral:WindowSeconds"] = "60"
        };
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString, overrides);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken());

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsync("/hubs/rides/negotiate?negotiateVersion=1", content: null);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
