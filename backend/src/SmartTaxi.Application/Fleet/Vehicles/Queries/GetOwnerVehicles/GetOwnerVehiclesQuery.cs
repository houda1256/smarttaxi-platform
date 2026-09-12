using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Vehicles;

namespace SmartTaxi.Application.Fleet.Vehicles.Queries.GetOwnerVehicles;

public sealed record GetOwnerVehiclesQuery(Guid RequestingUserId, Guid OwnerId) : IQuery<Result<IReadOnlyCollection<VehicleSummary>>>;
