using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleById;

public sealed record GetVehicleByIdQuery(Guid VehicleId) : IQuery<Result<VehicleSummary>>;
