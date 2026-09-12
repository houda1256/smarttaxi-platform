using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Disputes.Abstractions;
using SmartTaxi.Domain.Payments.Disputes.Entities;

namespace SmartTaxi.Application.Payments.Disputes.Queries.GetMyFinancialDisputes;

public sealed class GetMyFinancialDisputesQueryHandler : IQueryHandler<GetMyFinancialDisputesQuery, PagedResult<FinancialDispute>>
{
    private readonly IFinancialDisputeRepository _disputeRepository;

    public GetMyFinancialDisputesQueryHandler(IFinancialDisputeRepository disputeRepository)
    {
        _disputeRepository = disputeRepository;
    }

    public Task<PagedResult<FinancialDispute>> Handle(GetMyFinancialDisputesQuery query, CancellationToken cancellationToken) =>
        _disputeRepository.GetRaisedByUserAsync(query.RaisedBy, query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
