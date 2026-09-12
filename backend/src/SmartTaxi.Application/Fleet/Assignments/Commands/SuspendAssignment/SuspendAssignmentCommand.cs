using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.SuspendAssignment;

public sealed record SuspendAssignmentCommand(Guid RequestingUserId, Guid AssignmentId) : ICommand<Result>;
