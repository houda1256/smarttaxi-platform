using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleEligibility;

public sealed record GetVehicleEligibilityQuery(Guid VehicleId) : IQuery<Result<VehicleEligibilityReport>>;
