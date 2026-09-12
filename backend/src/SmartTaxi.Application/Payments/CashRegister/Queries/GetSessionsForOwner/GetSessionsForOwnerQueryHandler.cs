using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetSessionsForOwner;

public sealed class GetSessionsForOwnerQueryHandler : IQueryHandler<GetSessionsForOwnerQuery, PagedResult<CashRegisterSession>>
{
    private readonly ICashRegisterSessionRepository _sessionRepository;

    public GetSessionsForOwnerQueryHandler(ICashRegisterSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public Task<PagedResult<CashRegisterSession>> Handle(GetSessionsForOwnerQuery query, CancellationToken cancellationToken) =>
        _sessionRepository.GetForOwnerAsync(query.OwnerId, query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
