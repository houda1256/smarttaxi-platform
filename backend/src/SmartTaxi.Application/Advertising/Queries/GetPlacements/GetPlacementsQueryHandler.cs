using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPlacements;

public sealed class GetPlacementsQueryHandler : IQueryHandler<GetPlacementsQuery, IReadOnlyCollection<AdvertisingPlacement>>
{
    private readonly IAdvertisingPlacementRepository _repository;

    public GetPlacementsQueryHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AdvertisingPlacement>> Handle(GetPlacementsQuery query, CancellationToken cancellationToken) =>
        _repository.GetActiveAsync(cancellationToken);
}
