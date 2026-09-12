using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ActivateSos;

public sealed record ActivateSosCommand(Guid RequestingUserId, Guid RideId, double Latitude, double Longitude, string Reason)
    : ICommand<Result<Guid>>;
