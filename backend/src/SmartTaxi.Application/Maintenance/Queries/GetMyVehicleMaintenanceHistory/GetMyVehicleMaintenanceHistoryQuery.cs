using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Maintenance.Entities;

namespace SmartTaxi.Application.Maintenance.Queries.GetMyVehicleMaintenanceHistory;

public sealed record GetMyVehicleMaintenanceHistoryQuery(Guid VehicleId, Guid OwnerUserId) : IQuery<Result<IReadOnlyCollection<MaintenanceRecord>>>;
