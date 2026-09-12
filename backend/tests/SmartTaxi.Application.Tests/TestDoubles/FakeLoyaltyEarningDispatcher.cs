using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Application.Loyalty.Contracts;

namespace SmartTaxi.Application.Tests.TestDoubles;

public sealed class FakeLoyaltyEarningDispatcher : ILoyaltyEarningDispatcher
{
    public List<LoyaltyPaymentAwardRequest> AwardedRequests { get; } = [];

    public Task AwardForPaymentAsync(LoyaltyPaymentAwardRequest request, CancellationToken cancellationToken)
    {
        AwardedRequests.Add(request);
        return Task.CompletedTask;
    }
}
