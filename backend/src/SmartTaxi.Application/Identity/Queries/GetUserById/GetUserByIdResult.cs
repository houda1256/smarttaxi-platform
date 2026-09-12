namespace SmartTaxi.Application.Identity.Queries.GetUserById;

public sealed record GetUserByIdResult(
    Guid UserId,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
