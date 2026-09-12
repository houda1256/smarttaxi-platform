using Microsoft.Extensions.DependencyInjection;
using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Infrastructure.Advertising.Services;

namespace SmartTaxi.Infrastructure.Tests.Advertising.Services;

/// <summary>Exercises the real ASP.NET Core Data Protection wiring under an actual DI container (not the
/// Application-layer fake) — proves the cryptographic round trip, tamper rejection and expiration actually work,
/// not just their in-memory approximation (Module 8 audit fix #1).</summary>
public class AdDeliveryTokenServiceTests
{
    private sealed class FixedLifetimePolicy(TimeSpan lifetime) : IAdvertisingDeliveryTokenPolicy
    {
        public TimeSpan TokenLifetime { get; } = lifetime;
    }

    private static AdDeliveryTokenService CreateService(TimeSpan? lifetime = null)
    {
        var services = new ServiceCollection();
        services.AddDataProtection();
        var provider = services.BuildServiceProvider();
        return new AdDeliveryTokenService(
            provider.GetRequiredService<Microsoft.AspNetCore.DataProtection.IDataProtectionProvider>(),
            new FixedLifetimePolicy(lifetime ?? TimeSpan.FromMinutes(2)));
    }

    [Fact]
    public void IssueThenValidate_WithMatchingContext_Succeeds()
    {
        var service = CreateService();
        var campaignId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var utcNow = DateTime.UtcNow;

        var token = service.IssueToken(campaignId, placementId, userId, utcNow);
        var result = service.Validate(token, campaignId, placementId, userId, utcNow);

        Assert.True(result.IsValid);
        Assert.NotNull(result.DeliveryId);
    }

    [Fact]
    public void Validate_TamperedToken_IsRejected()
    {
        var service = CreateService();
        var token = service.IssueToken(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var result = service.Validate(tampered, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExpiredToken_IsRejected()
    {
        var service = CreateService(TimeSpan.FromMilliseconds(1));
        var campaignId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var issuedAt = DateTime.UtcNow;
        var token = service.IssueToken(campaignId, placementId, userId, issuedAt);

        Thread.Sleep(50);
        var result = service.Validate(token, campaignId, placementId, userId, DateTime.UtcNow);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DifferentCampaign_IsRejected()
    {
        var service = CreateService();
        var placementId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = service.IssueToken(Guid.NewGuid(), placementId, userId, DateTime.UtcNow);

        var result = service.Validate(token, Guid.NewGuid(), placementId, userId, DateTime.UtcNow);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_DifferentUser_IsRejected()
    {
        var service = CreateService();
        var campaignId = Guid.NewGuid();
        var placementId = Guid.NewGuid();
        var token = service.IssueToken(campaignId, placementId, Guid.NewGuid(), DateTime.UtcNow);

        var result = service.Validate(token, campaignId, placementId, Guid.NewGuid(), DateTime.UtcNow);

        Assert.False(result.IsValid);
    }
}
