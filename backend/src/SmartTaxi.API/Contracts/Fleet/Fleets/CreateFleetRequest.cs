namespace SmartTaxi.API.Contracts.Fleet.Fleets;

public sealed record CreateFleetRequest(string Name, string? Description, Guid CityId);
