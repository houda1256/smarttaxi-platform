using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class SecurityPolicy : ISecurityPolicy
{
    public bool RevokeOtherSessionsOnPasswordChange { get; }

    public SecurityPolicy(IOptions<SecurityOptions> options)
    {
        RevokeOtherSessionsOnPasswordChange = options.Value.RevokeOtherSessionsOnPasswordChange;
    }
}
