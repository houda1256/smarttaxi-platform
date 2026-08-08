using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashDeclarations.Abstractions;
using SmartTaxi.Domain.Payments.CashDeclarations.Entities;

namespace SmartTaxi.Application.Payments.CashDeclarations.Queries.GetCashDeclarationById;

public sealed class GetCashDeclarationByIdQueryHandler : IQueryHandler<GetCashDeclarationByIdQuery, Result<CashDeclaration>>
{
    private const string NotFoundError = "Déclaration de caisse introuvable.";

    private readonly ICashDeclarationRepository _declarationRepository;

    public GetCashDeclarationByIdQueryHandler(ICashDeclarationRepository declarationRepository)
    {
        _declarationRepository = declarationRepository;
    }

    public async Task<Result<CashDeclaration>> Handle(GetCashDeclarationByIdQuery query, CancellationToken cancellationToken)
    {
        var declaration = await _declarationRepository.GetByIdAsync(query.CashDeclarationId, cancellationToken);

        return declaration is null
            ? Result<CashDeclaration>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<CashDeclaration>.Success(declaration);
    }
}
