using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Identity.Entities;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class JwtTokenGenerator : ITokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IRolePermissionRepository _rolePermissionRepository;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IRolePermissionRepository rolePermissionRepository)
    {
        _options = options.Value;
        _rolePermissionRepository = rolePermissionRepository;

        if (string.IsNullOrWhiteSpace(_options.Key))
        {
            throw new InvalidOperationException(
                "La clé JWT ('Jwt:Key') n'est pas configurée. Définissez-la via dotnet user-secrets.");
        }
    }

    public async Task<string> GenerateToken(User user, Guid sessionId, CancellationToken cancellationToken)
    {
        var permissions = await _rolePermissionRepository.GetPermissionsForRolesAsync(
            user.Roles.ToList(), cancellationToken);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("sid", sessionId.ToString())
        };

        claims.AddRange(user.Roles.Select(role => new Claim("role", role.ToString())));
        claims.AddRange(permissions.Select(permission => new Claim(Permissions.ClaimType, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
