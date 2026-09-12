namespace SmartTaxi.API.Contracts.Identity;

public sealed record UserProfileResponse(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
