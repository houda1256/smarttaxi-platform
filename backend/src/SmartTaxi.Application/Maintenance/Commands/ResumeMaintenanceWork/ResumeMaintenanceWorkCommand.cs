using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.ResumeMaintenanceWork;

public sealed record ResumeMaintenanceWorkCommand(Guid RequestId, Guid GarageUserId) : ICommand<Result>;
