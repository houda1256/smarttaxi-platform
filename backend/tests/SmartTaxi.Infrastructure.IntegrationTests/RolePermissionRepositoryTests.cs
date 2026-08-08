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
        // and Financial Disputes permissions (seeded in Phase 5B).
        Assert.Equal(16, permissions.Count);
        Assert.Contains("professional.register.own", permissions);
        Assert.Contains("preferences.manage.own", permissions);
        Assert.Contains("data-requests.submit.own", permissions);
        Assert.Contains("rides.create", permissions);
        Assert.Contains("payments.create", permissions);
        Assert.Contains("finance.disputes.open.own", permissions);
        Assert.DoesNotContain("users.read", permissions);
        Assert.DoesNotContain("users.manage", permissions);
        Assert.DoesNotContain("rides.cancel.admin", permissions);
        Assert.DoesNotContain("payments.refund", permissions);
    }
}
