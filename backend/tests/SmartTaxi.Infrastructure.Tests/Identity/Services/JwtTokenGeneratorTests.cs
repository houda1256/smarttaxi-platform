using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Domain.Identity.Enums;
using SmartTaxi.Domain.Identity.ValueObjects;
using SmartTaxi.Infrastructure.Identity.Options;
using SmartTaxi.Infrastructure.Identity.Services;

namespace SmartTaxi.Infrastructure.Tests.Identity.Services;

public class JwtTokenGeneratorTests
{
    private sealed class StubRolePermissionRepository : IRolePermissionRepository
    {
        private readonly IReadOnlyCollection<string> _permissions;

        public StubRolePermissionRepository(IReadOnlyCollection<string> permissions) => _permissions = permissions;

        public Task<IReadOnlyCollection<string>> GetPermissionsForRolesAsync(
            IReadOnlyCollection<UserRole> roles, CancellationToken cancellationToken)
            => Task.FromResult(_permissions);
    }

    private static JwtOptions CreateJwtOptions(string key = "this-is-a-test-signing-key-that-is-long-enough-1234567890") => new()
    {
        Key = key,
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiryMinutes = 60
    };

    [Fact]
    public async Task GenerateToken_IncludesSessionIdAndAllCurrentRolesAndPermissions()
    {
        var user = User.Create(Email.Create("user@example.com"), HashedPassword.Create("hash"), UserRole.Admin);
        user.AssignRole(UserRole.Driver);
        var sessionId = Guid.NewGuid();

        var generator = new JwtTokenGenerator(
            Options.Create(CreateJwtOptions()),
            new StubRolePermissionRepository(["users.read", "users.manage"]));

        var token = await generator.GenerateToken(user, sessionId, CancellationToken.None);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claims = jwt.Claims.ToList();

        Assert.Equal(sessionId.ToString(), claims.Single(c => c.Type == "sid").Value);
        Assert.Equal(
            new[] { "Admin", "Driver" },
            claims.Where(c => c.Type == "role").Select(c => c.Value).OrderBy(v => v));
        Assert.Equal(
            new[] { "users.manage", "users.read" },
            claims.Where(c => c.Type == "permission").Select(c => c.Value).OrderBy(v => v));
    }

    [Fact]
    public void Constructor_WithMissingKey_Throws()
    {
        var options = CreateJwtOptions(key: "");

        Assert.Throws<InvalidOperationException>(
            () => new JwtTokenGenerator(Options.Create(options), new StubRolePermissionRepository([])));
    }
}
