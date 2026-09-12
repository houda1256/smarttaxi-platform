using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments.Abstractions;
using SmartTaxi.Domain.Fleet.Assignments.Entities;
using SmartTaxi.Domain.Fleet.Assignments.Enums;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;

/// <summary>Dry-run check — does not create anything, just reports whether a proposed slot would conflict.</summary>
public sealed class ValidateAssignmentAvailabilityQueryHandler
    : IQueryHandler<ValidateAssignmentAvailabilityQuery, AssignmentAvailabilityReport>
{
    private readonly IDriverVehicleAssignmentRepository _repository;

    public ValidateAssignmentAvailabilityQueryHandler(IDriverVehicleAssignmentRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssignmentAvailabilityReport> Handle(
        ValidateAssignmentAvailabilityQuery query, CancellationToken cancellationToken)
    {
        var candidate = DriverVehicleAssignment.CreateDraft(
            query.DriverId, query.VehicleId, Guid.Empty, query.StartDate, query.EndDate, null, null,
            query.DaysOfWeek, Guid.Empty, DateTime.UtcNow);

        var driverAssignments = await _repository.GetActiveOrPendingForDriverAsync(query.DriverId, null, cancellationToken);
        var driverConflict = driverAssignments.Any(existing => existing.OverlapsWith(candidate));

        var vehicleAssignments = await _repository.GetActiveOrPendingForVehicleAsync(query.VehicleId, null, cancellationToken);
        var vehicleConflict = vehicleAssignments.Any(existing => existing.OverlapsWith(candidate));

        return new AssignmentAvailabilityReport(!driverConflict && !vehicleConflict, driverConflict, vehicleConflict);
    }
}
