using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ApproveSharedRideByDriver;

public sealed record ApproveSharedRideByDriverCommand(Guid RequestingUserId, Guid MatchId, Guid VehicleId) : ICommand<Result>;
