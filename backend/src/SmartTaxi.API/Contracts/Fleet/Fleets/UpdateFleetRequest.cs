namespace SmartTaxi.API.Contracts.Fleet.Fleets;

public sealed record UpdateFleetRequest(string Name, string? Description, Guid CityId);
