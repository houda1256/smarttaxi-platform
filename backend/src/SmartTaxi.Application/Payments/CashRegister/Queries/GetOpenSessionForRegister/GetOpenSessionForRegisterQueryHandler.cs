using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.CashRegister.Abstractions;
using SmartTaxi.Domain.Payments.CashRegister.Entities;

namespace SmartTaxi.Application.Payments.CashRegister.Queries.GetOpenSessionForRegister;

public sealed class GetOpenSessionForRegisterQueryHandler : IQueryHandler<GetOpenSessionForRegisterQuery, CashRegisterSession?>
{
    private readonly ICashRegisterSessionRepository _sessionRepository;

    public GetOpenSessionForRegisterQueryHandler(ICashRegisterSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public Task<CashRegisterSession?> Handle(GetOpenSessionForRegisterQuery query, CancellationToken cancellationToken) =>
        _sessionRepository.GetOpenForRegisterAsync(query.CashRegisterId, cancellationToken);
}
