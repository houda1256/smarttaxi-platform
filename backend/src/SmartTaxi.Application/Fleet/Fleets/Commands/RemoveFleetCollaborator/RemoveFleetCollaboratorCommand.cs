using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Fleets.Commands.RemoveFleetCollaborator;

public sealed record RemoveFleetCollaboratorCommand(Guid RequestingUserId, Guid FleetId, Guid MemberId) : ICommand<Result>;
