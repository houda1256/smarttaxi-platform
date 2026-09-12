using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.Application.Rides.Commands.SubmitRideComplaint;

/// <summary>ConcernedUserId is derived server-side as "the other participant", same reasoning as SubmitRideRatingCommand.</summary>
public sealed record SubmitRideComplaintCommand(Guid RequestingUserId, Guid RideId, RideComplaintCategory Category, string Description)
    : ICommand<Result<Guid>>;
