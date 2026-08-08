using Microsoft.Extensions.Options;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Infrastructure.Payments.Options;

namespace SmartTaxi.Infrastructure.Payments.Policies;

internal sealed class PayoutPolicy : IPayoutPolicy
{
    public decimal MinimumPayoutAmount { get; }

    public PayoutPolicy(IOptions<PayoutOptions> options)
    {
        MinimumPayoutAmount = options.Value.MinimumPayoutAmount;
    }
}
