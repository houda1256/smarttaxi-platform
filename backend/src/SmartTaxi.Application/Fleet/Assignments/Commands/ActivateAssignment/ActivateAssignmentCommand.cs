using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;

public sealed record ActivateAssignmentCommand(Guid RequestingUserId, Guid AssignmentId) : ICommand<Result>;
