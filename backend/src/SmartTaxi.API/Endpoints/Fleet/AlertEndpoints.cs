using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Alerts;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Alerts.Commands.DismissFleetAlert;
using SmartTaxi.Application.Fleet.Alerts.Commands.GenerateFleetAlerts;
using SmartTaxi.Application.Fleet.Alerts.Commands.ResolveFleetAlert;
using SmartTaxi.Application.Fleet.Alerts.Queries.GetFleetAlerts;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Alerts.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class AlertEndpoints
{
    private const string InvalidStatusError = "Statut d'alerte inconnu.";

    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/alerts")
            .WithTags("FleetAlerts").RequireAuthorization(Permissions.FleetAlertsManageOwn);

        group.MapGet("/", GetMineAsync)
            .WithName("GetFleetAlerts")
            .Produces<IReadOnlyCollection<FleetAlertResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/generate", GenerateAsync)
            .WithName("GenerateFleetAlerts")
            .Produces<int>(StatusCodes.Status200OK);

        group.MapPost("/{alertId:guid}/resolve", ResolveAsync)
            .WithName("ResolveFleetAlert")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{alertId:guid}/dismiss", DismissAsync)
            .WithName("DismissFleetAlert")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyCollection<FleetAlertResponse>>, BadRequest<ProblemDetails>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetFleetAlertsQueryHandler handler, CancellationToken cancellationToken, string? status = null)
    {
        FleetAlertStatus? parsedStatus = null;

        if (status is not null)
        {
            if (!Enum.TryParse<FleetAlertStatus>(status, ignoreCase: true, out var statusValue))
            {
                return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidStatusError });
            }

            parsedStatus = statusValue;
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var alerts = await handler.Handle(new GetFleetAlertsQuery(ownerId, parsedStatus), cancellationToken);

        IReadOnlyCollection<FleetAlertResponse> response = alerts.Select(FleetAlertResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<int>> GenerateAsync(
        ClaimsPrincipal currentUser, GenerateFleetAlertsCommandHandler handler, CancellationToken cancellationToken)
    {
        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GenerateFleetAlertsCommand(ownerId), cancellationToken);

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ResolveAsync(
        Guid alertId, ClaimsPrincipal currentUser, ResolveFleetAlertCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ResolveFleetAlertCommand(userId, alertId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> DismissAsync(
        Guid alertId, ClaimsPrincipal currentUser, DismissFleetAlertCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new DismissFleetAlertCommand(userId, alertId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>> ToNoContentResult(Result result)
    {
        if (result.IsSuccess)
        {
            return TypedResults.NoContent();
        }

        var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
        return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.Conflict(problem);
    }
}
