using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleEligibility;

public sealed class GetVehicleEligibilityQueryHandler : IQueryHandler<GetVehicleEligibilityQuery, Result<VehicleEligibilityReport>>
{
    private const string NotFoundError = "Véhicule introuvable.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly VehicleEligibilityChecker _eligibilityChecker;

    public GetVehicleEligibilityQueryHandler(IVehicleRepository vehicleRepository, VehicleEligibilityChecker eligibilityChecker)
    {
        _vehicleRepository = vehicleRepository;
        _eligibilityChecker = eligibilityChecker;
    }

    public async Task<Result<VehicleEligibilityReport>> Handle(GetVehicleEligibilityQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<VehicleEligibilityReport>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var report = await _eligibilityChecker.CheckAsync(vehicle, DateTime.UtcNow, cancellationToken);

        return Result<VehicleEligibilityReport>.Success(report);
    }
}
