using Microsoft.Extensions.Options;
using SmartTaxi.Application.Payments.Abstractions;
using SmartTaxi.Infrastructure.Payments.Options;

namespace SmartTaxi.Infrastructure.Payments.Policies;

internal sealed class PlatformCommissionPolicy : IPlatformCommissionPolicy
{
    public decimal CommissionPercentage { get; }

    public PlatformCommissionPolicy(IOptions<PlatformCommissionOptions> options)
    {
        CommissionPercentage = options.Value.CommissionPercentage;
    }
}
