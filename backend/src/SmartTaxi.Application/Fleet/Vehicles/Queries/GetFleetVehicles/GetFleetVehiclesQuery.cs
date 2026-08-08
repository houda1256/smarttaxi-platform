using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetFleetVehicles;

public sealed record GetFleetVehiclesQuery(Guid RequestingUserId, Guid FleetId) : IQuery<Result<IReadOnlyCollection<VehicleSummary>>>;
