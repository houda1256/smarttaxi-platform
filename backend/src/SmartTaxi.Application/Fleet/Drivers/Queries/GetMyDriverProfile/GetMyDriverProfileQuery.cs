using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Fleet.Drivers;

namespace SmartTaxi.Application.Fleet.Drivers.Queries.GetMyDriverProfile;

public sealed record GetMyDriverProfileQuery(Guid UserId) : IQuery<Result<DriverProfileSummary>>;
