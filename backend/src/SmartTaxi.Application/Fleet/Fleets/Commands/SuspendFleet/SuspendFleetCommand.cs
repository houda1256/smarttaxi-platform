using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.SuspendFleet;

public sealed record SuspendFleetCommand(Guid RequestingUserId, Guid FleetId) : ICommand<Result>;
