using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.Application.Administration.Commands.ReactivateUser;
using SmartTaxi.Application.Administration.Commands.ResetUserTwoFactor;
using SmartTaxi.Application.Administration.Commands.RevokeUserSessions;
using SmartTaxi.Application.Administration.Commands.SuspendUser;
using SmartTaxi.Application.Identity.Authorization;

namespace SmartTaxi.API.Endpoints.Administration;

/// <summary>
/// Every action here administers ANOTHER account — self-targeting is rejected
/// by the command handlers, never treated as an alternative self-service path.
/// Another Admin may be targeted; no admin hierarchy exists in this codebase.
/// </summary>
public static class AdminUserManagementEndpoints
{
    public static IEndpointRouteBuilder MapAdminUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users").WithTags("Administration - User Management");

        group.MapPost("/{userId:guid}/suspend", SuspendUserAsync)
            .WithName("SuspendUser")
            .RequireAuthorization(Permissions.AdminUsersManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{userId:guid}/reactivate", ReactivateUserAsync)
            .WithName("ReactivateUser")
            .RequireAuthorization(Permissions.AdminUsersManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{userId:guid}/sessions/revoke", RevokeUserSessionsAsync)
            .WithName("RevokeUserSessions")
            .RequireAuthorization(Permissions.AdminUsersManage)
            .Produces<int>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{userId:guid}/two-factor/reset", ResetUserTwoFactorAsync)
            .WithName("ResetUserTwoFactor")
            .RequireAuthorization(Permissions.AdminUsersTwoFactorReset)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<NoContent, ProblemHttpResult>> SuspendUserAsync(
        Guid userId, ClaimsPrincipal currentUser, SuspendUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SuspendUserCommand(userId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReactivateUserAsync(
        Guid userId, ClaimsPrincipal currentUser, ReactivateUserCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReactivateUserCommand(userId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<int>, ProblemHttpResult>> RevokeUserSessionsAsync(
        Guid userId, ClaimsPrincipal currentUser, RevokeUserSessionsCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RevokeUserSessionsCommand(userId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResetUserTwoFactorAsync(
        Guid userId, ClaimsPrincipal currentUser, ResetUserTwoFactorCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResetUserTwoFactorCommand(userId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
