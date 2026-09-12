using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class PasswordResetPolicy : IPasswordResetPolicy
{
    public TimeSpan TokenLifetime { get; }

    public PasswordResetPolicy(IOptions<PasswordResetOptions> options)
    {
        TokenLifetime = TimeSpan.FromHours(options.Value.TokenLifetimeHours);
    }
}
