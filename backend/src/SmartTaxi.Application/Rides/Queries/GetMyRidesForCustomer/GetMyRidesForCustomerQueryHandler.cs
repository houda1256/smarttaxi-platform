using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Rides.Abstractions;

namespace SmartTaxi.Application.Rides.Queries.GetMyRidesForCustomer;

public sealed class GetMyRidesForCustomerQueryHandler : IQueryHandler<GetMyRidesForCustomerQuery, IReadOnlyCollection<RideSummary>>
{
    private readonly IRideRepository _repository;

    public GetMyRidesForCustomerQueryHandler(IRideRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyCollection<RideSummary>> Handle(GetMyRidesForCustomerQuery query, CancellationToken cancellationToken)
    {
        var rides = await _repository.GetForCustomerAsync(query.CustomerId, cancellationToken);

        return rides.Select(RideSummary.FromEntity).ToList();
    }
}
