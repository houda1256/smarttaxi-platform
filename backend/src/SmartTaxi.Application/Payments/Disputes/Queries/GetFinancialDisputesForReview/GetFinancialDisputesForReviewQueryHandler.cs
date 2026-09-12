using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetFinancialDisputesForReview;

public sealed class GetFinancialDisputesForReviewQueryHandler : IQueryHandler<GetFinancialDisputesForReviewQuery, PagedResult<FinancialDispute>>
{
    private readonly IFinancialDisputeRepository _disputeRepository;

    public GetFinancialDisputesForReviewQueryHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public Task<PagedResult<FinancialDispute>> Handle(GetFinancialDisputesForReviewQuery query, CancellationToken cancellationToken) =>
        _disputeRepository.GetForReviewAsync(query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
