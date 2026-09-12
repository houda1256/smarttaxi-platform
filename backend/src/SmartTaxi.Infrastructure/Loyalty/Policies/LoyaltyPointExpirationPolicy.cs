using Microsoft.Extensions.Options;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Infrastructure.Loyalty.Options;

namespace SmartTaxi.Infrastructure.Loyalty.Policies;

internal sealed class LoyaltyPointExpirationPolicy : ILoyaltyPointExpirationPolicy
{
    public int ExpirationMonths { get; }

    public LoyaltyPointExpirationPolicy(IOptions<LoyaltyOptions> options)
    {
        ExpirationMonths = options.Value.PointExpirationMonths;
    }
}
