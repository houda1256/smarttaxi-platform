using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetMyRidesForDriver;

public sealed record GetMyRidesForDriverQuery(Guid DriverProfileId) : IQuery<IReadOnlyCollection<RideSummary>>;
