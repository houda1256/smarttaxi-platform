using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.UpdateFleet;

public sealed record UpdateFleetCommand(Guid RequestingUserId, Guid FleetId, string Name, string? Description, Guid CityId)
    : ICommand<Result>;
