using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationsForReview;

public sealed class GetCashDeclarationsForReviewQueryHandler : IQueryHandler<GetCashDeclarationsForReviewQuery, PagedResult<CashDeclaration>>
{
    private readonly ICashDeclarationRepository _declarationRepository;

    public GetCashDeclarationsForReviewQueryHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public Task<PagedResult<CashDeclaration>> Handle(GetCashDeclarationsForReviewQuery query, CancellationToken cancellationToken) =>
        _declarationRepository.GetForReviewAsync(query.Status, query.PageNumber, query.PageSize, cancellationToken);
}
