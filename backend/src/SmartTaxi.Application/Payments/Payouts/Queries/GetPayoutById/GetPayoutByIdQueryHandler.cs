using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Payments.Payouts.Abstractions;
using SmartTaxi.Domain.Payments.Payouts.Entities;

namespace SmartTaxi.Application.Payments.Payouts.Queries.GetPayoutById;

public sealed class GetPayoutByIdQueryHandler : IQueryHandler<GetPayoutByIdQuery, Result<Payout>>
{
    private const string NotFoundError = "Versement introuvable.";

    private readonly IPayoutRepository _payoutRepository;

    public GetPayoutByIdQueryHandler(IPayoutRepository payoutRepository)
    {
        _payoutRepository = payoutRepository;
    }

    public async Task<Result<Payout>> Handle(GetPayoutByIdQuery query, CancellationToken cancellationToken)
    {
        var payout = await _payoutRepository.GetByIdAsync(query.PayoutId, cancellationToken);

        return payout is null ? Result<Payout>.Failure(NotFoundError, ErrorType.NotFound) : Result<Payout>.Success(payout);
    }
}
