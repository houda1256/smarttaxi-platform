using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetOwnerVehicles;

/// <summary>Self-service only — the requester must be the owner in question, never a client-supplied OwnerId trusted blindly.</summary>
public sealed class GetOwnerVehiclesQueryHandler : IQueryHandler<GetOwnerVehiclesQuery, Result<IReadOnlyCollection<VehicleSummary>>>
{
    private const string ForbiddenError = "Vous ne pouvez consulter que vos propres véhicules.";

    private readonly IVehicleRepository _repository;

    public GetOwnerVehiclesQueryHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyCollection<VehicleSummary>>> Handle(
        GetOwnerVehiclesQuery query, CancellationToken cancellationToken)
    {
        if (query.RequestingUserId != query.OwnerId)
        {
            return Result<IReadOnlyCollection<VehicleSummary>>.Failure(ForbiddenError, ErrorType.Forbidden);
        }

        var vehicles = await _repository.GetForOwnerAsync(query.OwnerId, cancellationToken);

        return Result<IReadOnlyCollection<VehicleSummary>>.Success(vehicles.Select(VehicleSummary.FromEntity).ToList());
    }
}
