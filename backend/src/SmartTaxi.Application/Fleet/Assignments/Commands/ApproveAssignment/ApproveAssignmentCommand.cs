using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;

public sealed record ApproveAssignmentCommand(Guid RequestingUserId, Guid AssignmentId) : ICommand<Result>;
