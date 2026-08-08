using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Fleet.Vehicles.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Entities;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;

public sealed class CreateAssignmentCommandHandler : ICommandHandler<CreateAssignmentCommand, Result<Guid>>
{
    private const string VehicleNotFoundError = "Véhicule introuvable pour ce propriétaire.";
    private const string DriverNotFoundError = "Profil chauffeur introuvable.";
    private const string DriverOverlapError = "Ce chauffeur a déjà une affectation active ou en attente sur cette période.";
    private const string VehicleOverlapError = "Ce véhicule a déjà une affectation active ou en attente sur cette période.";

    private readonly IVehicleRepository _vehicleRepository;
    private readonly IDriverProfileRepository _driverRepository;
    private readonly IDriverVehicleAssignmentRepository _assignmentRepository;

    public CreateAssignmentCommandHandler(
        IVehicleRepository vehicleRepository, IDriverProfileRepository driverRepository,
        IDriverVehicleAssignmentRepository assignmentRepository)
    {
        _vehicleRepository = vehicleRepository;
        _driverRepository = driverRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<Result<Guid>> Handle(CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        var vehicle = await _vehicleRepository.GetByIdAsync(command.VehicleId, cancellationToken);

        if (vehicle is null || vehicle.OwnerId != command.RequestingUserId)
        {
            return Result<Guid>.Failure(VehicleNotFoundError, ErrorType.NotFound);
        }

        var driver = await _driverRepository.GetByIdAsync(command.DriverId, cancellationToken);

        if (driver is null)
        {
            return Result<Guid>.Failure(DriverNotFoundError, ErrorType.NotFound);
        }

        var candidate = DriverVehicleAssignment.CreateDraft(
            command.DriverId, command.VehicleId, command.RequestingUserId, command.StartDate, command.EndDate,
            command.StartTime, command.EndTime, command.DaysOfWeek, command.RequestingUserId, DateTime.UtcNow);

        var driverAssignments = await _assignmentRepository.GetActiveOrPendingForDriverAsync(command.DriverId, null, cancellationToken);

        if (driverAssignments.Any(existing => existing.OverlapsWith(candidate)))
        {
            return Result<Guid>.Failure(DriverOverlapError, ErrorType.Conflict);
        }

        var vehicleAssignments = await _assignmentRepository.GetActiveOrPendingForVehicleAsync(command.VehicleId, null, cancellationToken);

        if (vehicleAssignments.Any(existing => existing.OverlapsWith(candidate)))
        {
            return Result<Guid>.Failure(VehicleOverlapError, ErrorType.Conflict);
        }

        await _assignmentRepository.AddAsync(candidate, cancellationToken);

        return Result<Guid>.Success(candidate.Id);
    }
}
