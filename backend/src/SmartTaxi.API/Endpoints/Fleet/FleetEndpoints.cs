using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Fleets;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Fleets.Commands.AddFleetCollaborator;
using SmartTaxi.Application.Fleet.Fleets.Commands.CreateFleet;
using SmartTaxi.Application.Fleet.Fleets.Commands.RemoveFleetCollaborator;
using SmartTaxi.Application.Fleet.Fleets.Commands.SuspendFleet;
using SmartTaxi.Application.Fleet.Fleets.Commands.UpdateFleet;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetById;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetFleetCollaborators;
using SmartTaxi.Application.Fleet.Fleets.Queries.GetMyFleets;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Fleets.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class FleetEndpoints
{
    private const string InvalidRoleError = "Rôle de collaborateur inconnu.";

    public static IEndpointRouteBuilder MapFleetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/fleets")
            .WithTags("Fleets").RequireAuthorization(Permissions.FleetManageOwn);

        group.MapPost("/", CreateAsync)
            .WithName("CreateFleet")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", GetMineAsync)
            .WithName("GetMyFleets")
            .Produces<IReadOnlyCollection<FleetResponse>>(StatusCodes.Status200OK);

        group.MapGet("/{fleetId:guid}", GetByIdAsync)
            .WithName("GetFleetById")
            .Produces<FleetResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{fleetId:guid}", UpdateAsync)
            .WithName("UpdateFleet")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{fleetId:guid}/suspend", SuspendAsync)
            .WithName("SuspendFleet")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{fleetId:guid}/collaborators", GetCollaboratorsAsync)
            .WithName("GetFleetCollaborators")
            .Produces<IReadOnlyCollection<FleetMemberResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{fleetId:guid}/collaborators", AddCollaboratorAsync)
            .WithName("AddFleetCollaborator")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{fleetId:guid}/collaborators/{memberId:guid}", RemoveCollaboratorAsync)
            .WithName("RemoveFleetCollaborator")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, ForbidHttpResult>> CreateAsync(
        CreateFleetRequest request, ClaimsPrincipal currentUser, CreateFleetCommandHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new CreateFleetCommand(ownerId, request.Name, request.Description, request.CityId), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.Forbidden ? TypedResults.Forbid() : TypedResults.BadRequest(problem);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Ok<IReadOnlyCollection<FleetResponse>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyFleetsQueryHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var summaries = await handler.Handle(new GetMyFleetsQuery(ownerId), cancellationToken);

        IReadOnlyCollection<FleetResponse> response = summaries.Select(FleetResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<FleetResponse>, NotFound<ProblemDetails>>> GetByIdAsync(
        Guid fleetId, ClaimsPrincipal currentUser, GetFleetByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetFleetByIdQuery(userId, fleetId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Flotte introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(FleetResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> UpdateAsync(
        Guid fleetId, UpdateFleetRequest request, ClaimsPrincipal currentUser,
        UpdateFleetCommandHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(
            new UpdateFleetCommand(ownerId, fleetId, request.Name, request.Description, request.CityId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Flotte introuvable", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid fleetId, ClaimsPrincipal currentUser, SuspendFleetCommandHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendFleetCommand(ownerId, fleetId), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.Conflict(problem);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<FleetMemberResponse>>, NotFound<ProblemDetails>>> GetCollaboratorsAsync(
        Guid fleetId, ClaimsPrincipal currentUser, GetFleetCollaboratorsQueryHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetFleetCollaboratorsQuery(ownerId, fleetId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Flotte introuvable", Detail = result.Error });
        }

        IReadOnlyCollection<FleetMemberResponse> response = result.Value!.Select(FleetMemberResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> AddCollaboratorAsync(
        Guid fleetId, AddFleetCollaboratorRequest request, ClaimsPrincipal currentUser,
        AddFleetCollaboratorCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FleetCollaboratorRole>(request.Role, ignoreCase: true, out var role))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidRoleError });
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new AddFleetCollaboratorCommand(ownerId, fleetId, request.CollaboratorUserId, role);
        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> RemoveCollaboratorAsync(
        Guid fleetId, Guid memberId, ClaimsPrincipal currentUser,
        RemoveFleetCollaboratorCommandHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RemoveFleetCollaboratorCommand(ownerId, fleetId, memberId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Introuvable", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }
}
