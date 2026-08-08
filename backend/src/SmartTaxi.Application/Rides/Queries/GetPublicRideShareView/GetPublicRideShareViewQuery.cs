using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;

/// <summary>Unauthenticated by design — possession of the raw token is the only credential.</summary>
public sealed record GetPublicRideShareViewQuery(string RawToken) : IQuery<Result<PublicRideShareView>>;
