using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetEligibleDrivers;

public sealed record GetEligibleDriversQuery : IQuery<IReadOnlyCollection<DriverProfileSummary>>;
