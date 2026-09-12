using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.UpdateDriverAvailabilityLocation;

/// <summary>Periodic position ping from a Driver who has no active Ride yet — feeds DriverRecommendationService's search.</summary>
public sealed record UpdateDriverAvailabilityLocationCommand(Guid RequestingUserId, double Latitude, double Longitude) : ICommand<Result>;
