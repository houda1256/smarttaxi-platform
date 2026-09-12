using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.CreateFleet;

public sealed record CreateFleetCommand(Guid OwnerId, string Name, string? Description, Guid CityId) : ICommand<Result<Guid>>;
