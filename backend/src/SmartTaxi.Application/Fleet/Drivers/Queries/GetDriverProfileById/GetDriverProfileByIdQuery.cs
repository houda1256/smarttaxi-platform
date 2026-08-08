using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetDriverProfileById;

public sealed record GetDriverProfileByIdQuery(Guid DriverProfileId) : IQuery<Result<DriverProfileSummary>>;
