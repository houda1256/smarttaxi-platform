using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetCashRegisterSessionById;

public sealed class GetCashRegisterSessionByIdQueryHandler : IQueryHandler<GetCashRegisterSessionByIdQuery, Result<CashRegisterSession>>
{
    private const string NotFoundError = "Session de caisse introuvable.";

    private readonly ICashRegisterSessionRepository _sessionRepository;

    public GetCashRegisterSessionByIdQueryHandler(ICashRegisterSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<Result<CashRegisterSession>> Handle(GetCashRegisterSessionByIdQuery query, CancellationToken cancellationToken)
    {
        var session = await _sessionRepository.GetByIdAsync(query.CashRegisterSessionId, cancellationToken);

        return session is null
            ? Result<CashRegisterSession>.Failure(NotFoundError, ErrorType.NotFound)
            : Result<CashRegisterSession>.Success(session);
    }
}
