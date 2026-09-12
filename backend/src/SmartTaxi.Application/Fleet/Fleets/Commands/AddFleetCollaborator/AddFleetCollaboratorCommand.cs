using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.AddFleetCollaborator;

public sealed record AddFleetCollaboratorCommand(
    Guid RequestingUserId, Guid FleetId, Guid CollaboratorUserId, FleetCollaboratorRole Role) : ICommand<Result<Guid>>;
