using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.GetVehicleAssignments;

public sealed record GetVehicleAssignmentsQuery(Guid VehicleId) : IQuery<IReadOnlyCollection<AssignmentSummary>>;
