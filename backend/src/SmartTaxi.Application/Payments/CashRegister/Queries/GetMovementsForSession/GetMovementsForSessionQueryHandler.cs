using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetMovementsForSession;

public sealed class GetMovementsForSessionQueryHandler : IQueryHandler<GetMovementsForSessionQuery, IReadOnlyCollection<CashMovement>>
{
    private readonly ICashMovementRepository _movementRepository;

    public GetMovementsForSessionQueryHandler(ICashMovementRepository movementRepository)
    {
        _movementRepository = movementRepository;
    }

    public Task<IReadOnlyCollection<CashMovement>> Handle(GetMovementsForSessionQuery query, CancellationToken cancellationToken) =>
        _movementRepository.GetForSessionAsync(query.CashRegisterSessionId, cancellationToken);
}
