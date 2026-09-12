using System.Net;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Administration.Enums;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Persistence;

namespace SmartTaxi.API.Tests;

/// <summary>
/// Non-HTTP execution supporting a null audit context is already proven by
/// SmartTaxi.Infrastructure.IntegrationTests' AdminUserManagementIntegrationTests
/// (which construct AdminUserManagementRepository with NullAuditContextAccessor
/// directly). This class proves the other half: a real HTTP-triggered admin
/// action actually populates CorrelationId/IpAddress/UserAgent through
/// HttpCorrelationAuditContextAccessor end to end.
/// </summary>
[Collection("SharedApiPostgres")]
public class AuditContextIntegrationTests
{
    private readonly SharedApiPostgresFixture _fixture;

    public AuditContextIntegrationTests(SharedApiPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AdminSuspend_ThroughRealHttpRequest_PopulatesCorrelationIdAndUserAgentOnTheAuditEntry()
    {
        await using var factory = new SmartTaxiApiFactory(_fixture.ConnectionString);
        var targetUserId = await SeedActiveUserAsync(factory);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwt.CreateToken(permissions: [Permissions.AdminUsersManage]));
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SmartTaxiApiTests/1.0");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/users/{targetUserId}/suspend");
        request.Headers.Add("X-Correlation-ID", "audit-context-test-id");

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = await context.AuditLogs.SingleAsync(e => e.TargetId == targetUserId && e.Action == AuditAction.AdminUserSuspended);

        Assert.Equal("audit-context-test-id", entry.CorrelationId);
        Assert.Equal("203.0.113.42", entry.IpAddress);
        Assert.NotNull(entry.UserAgent);
        Assert.Contains("SmartTaxiApiTests", entry.UserAgent);
    }

    private static async Task<Guid> SeedActiveUserAsync(SmartTaxiApiFactory factory)
    {
        var user = User.Create(
            Email.Create($"{Guid.NewGuid()}@example.com"), HashedPassword.Create("hash"), UserRole.Customer, DateTime.UtcNow);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }
}
