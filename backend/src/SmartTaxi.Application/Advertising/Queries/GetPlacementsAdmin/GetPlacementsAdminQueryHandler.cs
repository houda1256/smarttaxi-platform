using SmartTaxi.Application.Advertising.Abstractions;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Advertising.Entities;

namespace SmartTaxi.Application.Advertising.Queries.GetPlacementsAdmin;

public sealed class GetPlacementsAdminQueryHandler : IQueryHandler<GetPlacementsAdminQuery, IReadOnlyCollection<AdvertisingPlacement>>
{
    private readonly IAdvertisingPlacementRepository _repository;

    public GetPlacementsAdminQueryHandler(IAdvertisingPlacementRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<AdvertisingPlacement>> Handle(GetPlacementsAdminQuery query, CancellationToken cancellationToken) =>
        _repository.GetAllAsync(cancellationToken);
}
