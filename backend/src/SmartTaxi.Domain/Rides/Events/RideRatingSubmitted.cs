using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record RideRatingSubmitted(Guid RatingId, Guid RideId, Guid ReviewerId, Guid ReviewedUserId, DateTime OccurredAtUtc) : IDomainEvent;
