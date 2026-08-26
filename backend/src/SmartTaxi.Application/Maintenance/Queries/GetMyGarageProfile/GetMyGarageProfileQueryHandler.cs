using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyGarageProfile;

public sealed class GetMyGarageProfileQueryHandler : IQueryHandler<GetMyGarageProfileQuery, GarageProfile?>
{
    private readonly IGarageProfileRepository _repository;

    public GetMyGarageProfileQueryHandler(IGarageProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<GarageProfile?> Handle(GetMyGarageProfileQuery query, CancellationToken cancellationToken) =>
        _repository.GetByUserIdAsync(query.UserId, cancellationToken);
}
