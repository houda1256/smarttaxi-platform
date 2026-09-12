using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Loyalty.Abstractions;
using SmartTaxi.Domain.Loyalty.Entities;

namespace SmartTaxi.Application.Loyalty.Queries.GetMyPointLedger;

public sealed class GetMyPointLedgerQueryHandler : IQueryHandler<GetMyPointLedgerQuery, PagedResult<LoyaltyPointLedgerEntry>>
{
    private const int MaxPageSize = 100;

    private readonly ILoyaltyPointLedgerRepository _ledgerRepository;

    public GetMyPointLedgerQueryHandler(ILoyaltyPointLedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public Task<PagedResult<LoyaltyPointLedgerEntry>> Handle(GetMyPointLedgerQuery query, CancellationToken cancellationToken)
    {
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize is < 1 or > MaxPageSize ? MaxPageSize : query.PageSize;

        return _ledgerRepository.GetForUserAsync(query.UserId, pageNumber, pageSize, cancellationToken);
    }
}
