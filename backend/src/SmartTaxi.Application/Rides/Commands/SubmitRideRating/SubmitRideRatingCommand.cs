using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.SubmitRideRating;

/// <summary>ReviewedUserId is never client-supplied — it's derived server-side as "the other participant" to prevent rating a third party.</summary>
public sealed record SubmitRideRatingCommand(Guid RequestingUserId, Guid RideId, int Score, string? Comment, string? Tags)
    : ICommand<Result<Guid>>;
