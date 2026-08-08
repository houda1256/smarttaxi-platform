using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class EmailVerificationPolicy : IEmailVerificationPolicy
{
    public TimeSpan TokenLifetime { get; }

    public TimeSpan ResendInterval { get; }

    public EmailVerificationPolicy(IOptions<EmailVerificationOptions> options)
    {
        var value = options.Value;
        TokenLifetime = TimeSpan.FromHours(value.TokenLifetimeHours);
        ResendInterval = TimeSpan.FromSeconds(value.ResendIntervalSeconds);
    }
}
