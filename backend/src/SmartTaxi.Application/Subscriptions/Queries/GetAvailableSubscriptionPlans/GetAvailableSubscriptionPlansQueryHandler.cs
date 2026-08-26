using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetAvailableSubscriptionPlans;

public sealed class GetAvailableSubscriptionPlansQueryHandler
    : IQueryHandler<GetAvailableSubscriptionPlansQuery, IReadOnlyCollection<SubscriptionPlan>>
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetAvailableSubscriptionPlansQueryHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public Task<IReadOnlyCollection<SubscriptionPlan>> Handle(
        GetAvailableSubscriptionPlansQuery query, CancellationToken cancellationToken) =>
        _planRepository.GetActiveByRoleAsync(query.TargetRole, cancellationToken);
}
