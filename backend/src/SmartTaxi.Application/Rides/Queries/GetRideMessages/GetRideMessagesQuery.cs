using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetRideMessages;

public sealed record GetRideMessagesQuery(Guid RequestingUserId, Guid RideId) : IQuery<Result<IReadOnlyCollection<RideMessageSummary>>>;
