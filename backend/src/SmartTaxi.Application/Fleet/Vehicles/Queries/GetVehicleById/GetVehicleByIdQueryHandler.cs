using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleById;

public sealed class GetVehicleByIdQueryHandler : IQueryHandler<GetVehicleByIdQuery, Result<VehicleSummary>>
{
    private const string NotFoundError = "Véhicule introuvable.";

    private readonly IVehicleRepository _repository;

    public GetVehicleByIdQueryHandler(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<VehicleSummary>> Handle(GetVehicleByIdQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await _repository.GetByIdAsync(query.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<VehicleSummary>.Failure(NotFoundError, ErrorType.NotFound);
        }

        return Result<VehicleSummary>.Success(VehicleSummary.FromEntity(vehicle));
    }
}
