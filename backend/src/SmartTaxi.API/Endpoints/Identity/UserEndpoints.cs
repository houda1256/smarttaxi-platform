using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Identity;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Identity.Commands.AssignRole;
using SmartTaxi.Application.Identity.Commands.RemoveRole;
using SmartTaxi.Application.Identity.Commands.RevokeSession;
using SmartTaxi.Application.Identity.Queries.GetUserById;
using SmartTaxi.Application.Identity.Queries.GetUserSessions;

namespace SmartTaxi.API.Endpoints.Identity;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync)
            .WithName("GetCurrentUser")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{userId:guid}", GetUserByIdAsync)
            .WithName("GetUserById")
            .RequireAuthorization(Permissions.UsersRead)
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{userId:guid}/roles", AssignRoleAsync)
            .WithName("AssignRole")
            .RequireAuthorization(Permissions.UsersManage)
            .Produces<AssignRoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{userId:guid}/roles/{role}", RemoveRoleAsync)
            .WithName("RemoveRole")
            .RequireAuthorization(Permissions.UsersManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/me/sessions", GetCurrentUserSessionsAsync)
            .WithName("GetCurrentUserSessions")
            .Produces<IReadOnlyCollection<SessionSummaryResponse>>(StatusCodes.Status200OK);

        group.MapDelete("/me/sessions/{sessionId:guid}", RevokeCurrentUserSessionAsync)
            .WithName("RevokeCurrentUserSession")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Task<Results<Ok<UserProfileResponse>, NotFound<ProblemDetails>>> GetCurrentUserAsync(
        ClaimsPrincipal currentUser,
        GetUserByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        return GetUserProfileAsync(userId, handler, cancellationToken);
    }

    private static Task<Results<Ok<UserProfileResponse>, NotFound<ProblemDetails>>> GetUserByIdAsync(
        Guid userId,
        GetUserByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return GetUserProfileAsync(userId, handler, cancellationToken);
    }

    private static async Task<Results<Ok<UserProfileResponse>, NotFound<ProblemDetails>>> GetUserProfileAsync(
        Guid userId,
        GetUserByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetUserByIdQuery(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Utilisateur introuvable",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        var value = result.Value!;
        var response = new UserProfileResponse(value.UserId, value.Email, value.Roles, value.Permissions);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<AssignRoleResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> AssignRoleAsync(
        Guid userId,
        AssignRoleRequest request,
        AssignRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AssignRoleCommand(userId, request.Role), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound
                ? TypedResults.NotFound(problem)
                : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(new AssignRoleResponse(result.Value!.UserId, result.Value.Roles));
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> RemoveRoleAsync(
        Guid userId,
        string role,
        RemoveRoleCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RemoveRoleCommand(userId, role), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound
                ? TypedResults.NotFound(problem)
                : TypedResults.BadRequest(problem);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyCollection<SessionSummaryResponse>>> GetCurrentUserSessionsAsync(
        ClaimsPrincipal currentUser,
        GetUserSessionsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetUserSessionsQuery(userId), cancellationToken);

        var response = result.Value!
            .Select(s => new SessionSummaryResponse(s.SessionId, s.CreatedAt, s.LastActivityAt, s.ExpiresAt, s.DeviceLabel, s.IsActive))
            .ToList();

        return TypedResults.Ok<IReadOnlyCollection<SessionSummaryResponse>>(response);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> RevokeCurrentUserSessionAsync(
        Guid sessionId,
        ClaimsPrincipal currentUser,
        RevokeSessionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RevokeSessionCommand(userId, sessionId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Session introuvable",
                Detail = result.Error,
                Status = StatusCodes.Status404NotFound
            });
        }

        return TypedResults.NoContent();
    }
}
