using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class RefreshTokenPolicy : IRefreshTokenPolicy
{
    public TimeSpan TokenLifetime { get; }

    public TimeSpan SessionLifetime { get; }

    public RefreshTokenPolicy(IOptions<RefreshTokenOptions> options)
    {
        var value = options.Value;
        TokenLifetime = TimeSpan.FromMinutes(value.LifetimeMinutes);
        SessionLifetime = TimeSpan.FromDays(value.SessionLifetimeDays);
    }
}
