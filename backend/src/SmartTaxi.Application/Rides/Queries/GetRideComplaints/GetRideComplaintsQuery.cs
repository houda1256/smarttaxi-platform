using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Rides.Entities;

namespace SmartTaxi.Application.Rides.Queries.GetRideComplaints;

public sealed record GetRideComplaintsQuery(Guid RideId) : IQuery<Result<IReadOnlyCollection<RideComplaint>>>;
