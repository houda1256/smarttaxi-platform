using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Maintenance.Commands.CreateMaintenanceRequest;

public sealed record CreateMaintenanceRequestCommand(Guid OwnerUserId, Guid VehicleId, Guid GarageUserId, string Description) : ICommand<Result<Guid>>;
