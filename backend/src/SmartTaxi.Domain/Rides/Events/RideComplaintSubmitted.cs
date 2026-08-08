using SmartTaxi.Domain.Common;

namespace SmartTaxi.Domain.Rides.Events;

public sealed record RideComplaintSubmitted(Guid ComplaintId, Guid RideId, Guid ComplainantUserId, DateTime OccurredAtUtc) : IDomainEvent;
