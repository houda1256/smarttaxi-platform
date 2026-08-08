using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.Application.Payments.CashDeclarations.Queries.GetMyCashDeclarations;

public sealed class GetMyCashDeclarationsQueryHandler : IQueryHandler<GetMyCashDeclarationsQuery, PagedResult<CashDeclaration>>
{
    private readonly ICashDeclarationRepository _declarationRepository;

    public GetMyCashDeclarationsQueryHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public Task<PagedResult<CashDeclaration>> Handle(GetMyCashDeclarationsQuery query, CancellationToken cancellationToken) =>
        _declarationRepository.GetForDriverAsync(query.DriverId, query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
