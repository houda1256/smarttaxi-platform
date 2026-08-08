namespace SmartTaxi.API.Contracts.Fleet.Fleets;

public sealed record AddFleetCollaboratorRequest(Guid CollaboratorUserId, string Role);
