using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.StartMaintenanceWork;

public sealed record StartMaintenanceWorkCommand(Guid RequestId, Guid GarageUserId) : ICommand<Result>;
