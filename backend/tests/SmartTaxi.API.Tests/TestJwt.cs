using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Tests;

/// <summary>Mints tokens signed with the same test key SmartTaxiApiFactory configures — mirrors JwtTokenGenerator's claim shape without needing a real login flow.</summary>
internal static class TestJwt
{
    public static string CreateToken(Guid? userId = null, IEnumerable<string>? permissions = null, TimeSpan? lifetime = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("sid", Guid.NewGuid().ToString())
        };

        foreach (var permission in permissions ?? [])
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SmartTaxiApiFactory.TestJwtKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        // Must match appsettings.json's actual Jwt:Issuer/Jwt:Audience values
        // ("SmartTaxi"), not a test-only override — see SmartTaxiApiFactory's
        // doc comment: Program.cs's AddJwtBearer configuration is built from
        // an eager, pre-Build() snapshot of these two specific keys, which a
        // WebApplicationFactory ConfigureAppConfiguration override does not
        // reach. Jwt:Key has no such conflict (appsettings.json sets no key
        // at all), so SmartTaxiApiFactory's Key override works normally.
        var token = new JwtSecurityToken(
            issuer: "SmartTaxi",
            audience: "SmartTaxi",
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(30)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
