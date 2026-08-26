using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Application.Maintenance.Abstractions;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyVehicleMaintenanceHistory;

/// <summary>Ownership re-checked server-side against Fleet's own IVehicleRepository — an owner must never read another owner's vehicle history by guessing a VehicleId.</summary>
public sealed class GetMyVehicleMaintenanceHistoryQueryHandler
    : IQueryHandler<GetMyVehicleMaintenanceHistoryQuery, Result<IReadOnlyCollection<MaintenanceRecord>>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable.";
    private const string NotOwnerError = "Vous ne pouvez consulter l'historique que de vos propres véhicules.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly IMaintenanceRecordRepository _recordRepository;

    public GetMyVehicleMaintenanceHistoryQueryHandler(IVehicleRepository vehicleRepository, IMaintenanceRecordRepository recordRepository)
    {
        _vehicleRepository = vehicleRepository;
        _recordRepository = recordRepository;
    }

    public async Task<Result<IReadOnlyCollection<MaintenanceRecord>>> Handle(
        GetMyVehicleMaintenanceHistoryQuery query, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(query.VehicleId, cancellationToken);

        if (vehicle is null)
        {
            return Result<IReadOnlyCollection<MaintenanceRecord>>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        if (vehicle.OwnerId != query.OwnerUserId)
        {
            return Result<IReadOnlyCollection<MaintenanceRecord>>.Failure(NotOwnerError, ErrorType.Forbidden);
        }

        var history = await _recordRepository.GetForVehicleAsync(query.VehicleId, cancellationToken);
        return Result<IReadOnlyCollection<MaintenanceRecord>>.Success(history);
    }
}
