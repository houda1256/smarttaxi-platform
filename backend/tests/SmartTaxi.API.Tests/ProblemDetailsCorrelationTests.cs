using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Tests;

/// <summary>
/// Proves — rather than assumes — that AddProblemDetails().CustomizeProblemDetails
/// actually runs for the TypedResults.Problem() path every module's ToProblem()
/// convention uses, by exercising a real Result-failure response end to end.
/// </summary>
[Collection("SharedApiPostgres")]
public class ProblemDetailsCorrelationTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public ProblemDetailsCorrelationTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task NormalResultFailure_ThroughToProblemConvention_IncludesCorrelationId()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: [Permissions.AdminUsersManage]));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/users/{Guid.NewGuid()}/suspend");
        request.Headers.Add("X-Correlation-ID", "known-correlation-id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(problem.TryGetProperty("correlationId", out var correlationId));
        Assert.Equal("known-correlation-id", correlationId.GetString());
    }
}
