using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CancelRideByAdmin;

public sealed record CancelRideByAdminCommand(Guid AdminUserId, Guid RideId, string? Reason) : ICommand<Result>;
