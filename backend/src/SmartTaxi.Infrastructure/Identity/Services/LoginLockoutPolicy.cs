using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class LoginLockoutPolicy : ILoginLockoutPolicy
{
    public int MaxFailedAttempts { get; }

    public TimeSpan LockoutDuration { get; }

    public LoginLockoutPolicy(IOptions<LoginLockoutOptions> options)
    {
        var value = options.Value;
        MaxFailedAttempts = value.MaxFailedAttempts;
        LockoutDuration = TimeSpan.FromMinutes(value.LockoutMinutes);
    }
}
