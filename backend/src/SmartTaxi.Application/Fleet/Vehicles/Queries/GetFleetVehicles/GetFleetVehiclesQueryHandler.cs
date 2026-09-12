using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Common.Abstractions;
using SmartTaxi.Application.Fleet.Fleets.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetFleetVehicles;

public sealed class GetFleetVehiclesQueryHandler : IQueryHandler<GetFleetVehiclesQuery, Result<IReadOnlyCollection<VehicleSummary>>>
{
    private const string NotFoundError = "Flotte introuvable.";

    private readonly IFleetRepository _fleetRepository;
    private readonly IFleetMemberRepository _memberRepository;
    private readonly IVehicleRepository _vehicleRepository;

    public GetFleetVehiclesQueryHandler(
        IFleetRepository fleetRepository, IFleetMemberRepository memberRepository, IVehicleRepository vehicleRepository)
    {
        _fleetRepository = fleetRepository;
        _memberRepository = memberRepository;
        _vehicleRepository = vehicleRepository;
    }

    public async Task<Result<IReadOnlyCollection<VehicleSummary>>> Handle(
        GetFleetVehiclesQuery query, CancellationToken cancellationToken)
    {
        var fleet = await _fleetRepository.GetByIdAsync(query.FleetId, cancellationToken);

        if (fleet is null)
        {
            return Result<IReadOnlyCollection<VehicleSummary>>.Failure(NotFoundError, ErrorType.NotFound);
        }

        if (fleet.OwnerId != query.RequestingUserId)
        {
            var membership = await _memberRepository.GetMembershipAsync(query.FleetId, query.RequestingUserId, cancellationToken);

            if (membership is null)
            {
                return Result<IReadOnlyCollection<VehicleSummary>>.Failure(NotFoundError, ErrorType.NotFound);
            }
        }

        var vehicles = await _vehicleRepository.GetForFleetAsync(query.FleetId, cancellationToken);

        return Result<IReadOnlyCollection<VehicleSummary>>.Success(vehicles.Select(VehicleSummary.FromEntity).ToList());
    }
}
