using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Assignments;

namespace SmartTaxi.Application.Fleet.Assignments.Queries.GetDriverAssignments;

public sealed record GetDriverAssignmentsQuery(Guid DriverId) : IQuery<IReadOnlyCollection<AssignmentSummary>>;
