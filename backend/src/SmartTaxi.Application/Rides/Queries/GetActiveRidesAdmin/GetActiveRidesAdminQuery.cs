using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetActiveRidesAdmin;

public sealed record GetActiveRidesAdminQuery : IQuery<IReadOnlyCollection<RideSummary>>;
