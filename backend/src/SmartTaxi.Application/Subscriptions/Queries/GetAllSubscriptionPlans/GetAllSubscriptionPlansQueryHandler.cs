using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Subscriptions.Abstractions;
using SmartTaxi.Domain.Subscriptions.Entities;

namespace SmartTaxi.Application.Subscriptions.Queries.GetAllSubscriptionPlans;

public sealed class GetAllSubscriptionPlansQueryHandler : IQueryHandler<GetAllSubscriptionPlansQuery, PagedResult<SubscriptionPlan>>
{
    private readonly ISubscriptionPlanRepository _planRepository;

    public GetAllSubscriptionPlansQueryHandler(ISubscriptionPlanRepository planRepository)
    {
        _planRepository = planRepository;
    }

    public Task<PagedResult<SubscriptionPlan>> Handle(GetAllSubscriptionPlansQuery query, CancellationToken cancellationToken) =>
        _planRepository.GetAllAsync(query.PageNumber, query.PageSize, cancellationToken);
}
