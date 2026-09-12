using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.CreateRideShareToken;

/// <summary>Returns the raw token — the only moment it is ever visible; only its hash is persisted.</summary>
public sealed record CreateRideShareTokenCommand(Guid RequestingUserId, Guid RideId) : ICommand<Result<string>>;
