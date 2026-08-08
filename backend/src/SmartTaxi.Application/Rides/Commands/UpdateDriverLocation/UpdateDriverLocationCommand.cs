using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.UpdateDriverLocation;

public sealed record UpdateDriverLocationCommand(
    Guid RequestingUserId, Guid RideId, double Latitude, double Longitude, double? Speed, double? Heading,
    double? Accuracy) : ICommand<Result>;
