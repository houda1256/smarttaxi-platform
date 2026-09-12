using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetSubscriptionPlanById;

public sealed class GetSubscriptionPlanByIdQueryHandler : IQueryHandler<GetSubscriptionPlanByIdQuery, SubscriptionPlan?>
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetSubscriptionPlanByIdQueryHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public Task<SubscriptionPlan?> Handle(GetSubscriptionPlanByIdQuery query, CancellationToken cancellationToken) =>
        _planRepository.GetByIdAsync(query.PlanId, cancellationToken);
}
