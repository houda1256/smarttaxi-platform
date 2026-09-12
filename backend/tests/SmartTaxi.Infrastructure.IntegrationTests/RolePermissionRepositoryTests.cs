using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Infrastructure.Identity.Repositories;

namespace SmartTaxi.Infrastructure.IntegrationTests;

[Collection("SharedPostgres")]
public class RolePermissionRepositoryTests
{
    private readonly SharedPostgresFixture _fixture;

    public RolePermissionRepositoryTests(SharedPostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPermissionsForRolesAsync_ForAdmin_ReturnsSeededDefaultPermissions()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RolePermissionRepository(context);

        var permissions = await repository.GetPermissionsForRolesAsync([UserRole.Admin], CancellationToken.None);

        Assert.Contains("users.read", permissions);
        Assert.Contains("users.manage", permissions);
    }

    [Fact]
    public async Task GetPermissionsForRolesAsync_ForCustomer_ReturnsOnlyBaselineSelfServicePermissions()
    {
        await using var context = _fixture.CreateContext();
        var repository = new RolePermissionRepository(context);

        var permissions = await repository.GetPermissionsForRolesAsync([UserRole.Customer], CancellationToken.None);

        // Least privilege: Customer gets no elevated/admin permissions, only the
        // baseline self-service ones every account gets (seeded in 2e) plus the
        // Customer-side Ride permissions (seeded in 4l), Payments permissions (seeded in 5h),
        // Financial Disputes permissions (seeded in Phase 5B), Subscription self-service
        // permissions (Module 5), Notifications self-service permissions (Module 6),
        // Loyalty self-service permissions (Module 7), the Advertising ad-tracking
        // permission (Module 8 — Customer's own app can view/click a served ad), and the
        // Support ticket self-service permissions (Module 11 — Customer never gets the
        // incidents.* permissions, which are Admin-only).
        Assert.Equal(32, permissions.Count);
        Assert.Contains("support.tickets.create.own", permissions);
        Assert.Contains("support.tickets.read.own", permissions);
        Assert.Contains("support.tickets.manage.own", permissions);
        Assert.DoesNotContain("support.tickets.read.all", permissions);
        Assert.DoesNotContain("support.tickets.manage.all", permissions);
        Assert.DoesNotContain("support.incidents.read.all", permissions);
        Assert.DoesNotContain("support.incidents.manage.all", permissions);
        Assert.Contains("professional.register.own", permissions);
        Assert.Contains("preferences.manage.own", permissions);
        Assert.Contains("data-requests.submit.own", permissions);
        Assert.Contains("rides.create", permissions);
        Assert.Contains("payments.create", permissions);
        Assert.Contains("finance.disputes.open.own", permissions);
        Assert.Contains("subscription.read.own", permissions);
        Assert.Contains("notifications.read.own", permissions);
        Assert.Contains("notifications.preferences.manage.own", permissions);
        Assert.Contains("notifications.device-tokens.manage.own", permissions);
        Assert.Contains("loyalty.account.read.own", permissions);
        Assert.Contains("loyalty.rewards.read", permissions);
        Assert.Contains("loyalty.redemption.create.own", permissions);
        Assert.Contains("loyalty.challenges.read", permissions);
        Assert.Contains("advertising.tracking.record", permissions);
        Assert.DoesNotContain("users.read", permissions);
        Assert.DoesNotContain("users.manage", permissions);
        Assert.DoesNotContain("rides.cancel.admin", permissions);
        Assert.DoesNotContain("payments.refund", permissions);
        Assert.DoesNotContain("advertising.campaigns.manage.own", permissions);
    }
}
