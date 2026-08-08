using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.CancelAssignment;

public sealed record CancelAssignmentCommand(Guid RequestingUserId, Guid AssignmentId) : ICommand<Result>;
