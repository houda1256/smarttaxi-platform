using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Support;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Support.Commands.AcknowledgeSupportIncident;
using SmartTaxi.Application.Support.Commands.CloseSupportIncident;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromFinancialDispute;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromMaintenance;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRide;
using SmartTaxi.Application.Support.Commands.CreateIncidentFromRoadside;
using SmartTaxi.Application.Support.Commands.InvestigateSupportIncident;
using SmartTaxi.Application.Support.Commands.MarkIncidentFalsePositive;
using SmartTaxi.Application.Support.Commands.ReassignSupportIncident;
using SmartTaxi.Application.Support.Commands.ReopenSupportIncident;
using SmartTaxi.Application.Support.Commands.ReportSupportIncident;
using SmartTaxi.Application.Support.Commands.ResolveSupportIncident;
using SmartTaxi.Application.Support.Queries.GetAllSupportIncidents;
using SmartTaxi.Application.Support.Queries.GetSupportIncidentById;

namespace SmartTaxi.API.Endpoints.Support;

/// <summary>
/// Entirely admin-only — incidents have no requester-facing side in this pass
/// (approved scope). The four CreateIncidentFrom* routes are manual,
/// admin-triggered escalations from another module's own record — never an
/// automatic hook fired by that module itself (approved decision).
/// </summary>
public static class SupportIncidentAdminEndpoints
{
    private const int DefaultPageSize = 20;

    public static IEndpointRouteBuilder MapSupportIncidentAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var incidents = app.MapGroup("/api/admin/support/incidents").WithTags("Support Admin - Incidents")
            .RequireAuthorization(Permissions.SupportIncidentsReadAll);

        incidents.MapGet("/", GetAllSupportIncidentsAsync).WithName("GetAllSupportIncidents")
            .Produces<PagedResult<SupportIncidentResponse>>(StatusCodes.Status200OK);

        incidents.MapGet("/{incidentId:guid}", GetSupportIncidentByIdAsync).WithName("GetSupportIncidentById")
            .Produces<SupportIncidentResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        var writes = app.MapGroup("/api/admin/support/incidents").WithTags("Support Admin - Incidents")
            .RequireAuthorization(Permissions.SupportIncidentsManageAll);

        writes.MapPost("/", ReportSupportIncidentAsync).WithName("ReportSupportIncident")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        writes.MapPost("/from-ride/{rideId:guid}", CreateIncidentFromRideAsync).WithName("CreateIncidentFromRide")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        writes.MapPost("/from-financial-dispute/{disputeId:guid}", CreateIncidentFromFinancialDisputeAsync)
            .WithName("CreateIncidentFromFinancialDispute").Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        writes.MapPost("/from-roadside/{requestId:guid}", CreateIncidentFromRoadsideAsync).WithName("CreateIncidentFromRoadside")
            .Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        writes.MapPost("/from-maintenance/{requestId:guid}", CreateIncidentFromMaintenanceAsync)
            .WithName("CreateIncidentFromMaintenance").Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        writes.MapPost("/{incidentId:guid}/acknowledge", AcknowledgeSupportIncidentAsync).WithName("AcknowledgeSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/reassign", ReassignSupportIncidentAsync).WithName("ReassignSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/investigate", InvestigateSupportIncidentAsync).WithName("InvestigateSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/resolve", ResolveSupportIncidentAsync).WithName("ResolveSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/close", CloseSupportIncidentAsync).WithName("CloseSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/reopen", ReopenSupportIncidentAsync).WithName("ReopenSupportIncident")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        writes.MapPost("/{incidentId:guid}/false-positive", MarkIncidentFalsePositiveAsync).WithName("MarkIncidentFalsePositive")
            .Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<PagedResult<SupportIncidentResponse>>> GetAllSupportIncidentsAsync(
        GetAllSupportIncidentsQueryHandler handler, CancellationToken cancellationToken, int pageNumber = 1, int pageSize = DefaultPageSize)
    {
        var result = await handler.Handle(new GetAllSupportIncidentsQuery(pageNumber, pageSize), cancellationToken);
        var response = new PagedResult<SupportIncidentResponse>(
            result.Items.Select(SupportIncidentResponse.FromEntity).ToList(), result.TotalCount, result.PageNumber, result.PageSize);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<SupportIncidentResponse>, ProblemHttpResult>> GetSupportIncidentByIdAsync(
        Guid incidentId, GetSupportIncidentByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSupportIncidentByIdQuery(incidentId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(SupportIncidentResponse.FromEntity(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> ReportSupportIncidentAsync(
        ReportSupportIncidentRequest request, ClaimsPrincipal currentUser, ReportSupportIncidentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ReportSupportIncidentCommand(
                CurrentUserId(currentUser), request.Type, request.Severity, request.Title, request.Description,
                request.RelatedEntityType, request.RelatedEntityId, request.Latitude, request.Longitude),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateIncidentFromRideAsync(
        Guid rideId, CreateIncidentFromRideRequest request, ClaimsPrincipal currentUser, CreateIncidentFromRideCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateIncidentFromRideCommand(rideId, CurrentUserId(currentUser), request.Type, request.Severity, request.DescriptionOverride),
            cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateIncidentFromFinancialDisputeAsync(
        Guid disputeId, CreateIncidentFromFinancialDisputeRequest request, ClaimsPrincipal currentUser,
        CreateIncidentFromFinancialDisputeCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateIncidentFromFinancialDisputeCommand(disputeId, CurrentUserId(currentUser), request.Severity), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateIncidentFromRoadsideAsync(
        Guid requestId, CreateIncidentFromRoadsideRequest request, ClaimsPrincipal currentUser,
        CreateIncidentFromRoadsideCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateIncidentFromRoadsideCommand(requestId, CurrentUserId(currentUser), request.Severity), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateIncidentFromMaintenanceAsync(
        Guid requestId, CreateIncidentFromMaintenanceRequest request, ClaimsPrincipal currentUser,
        CreateIncidentFromMaintenanceCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CreateIncidentFromMaintenanceCommand(requestId, CurrentUserId(currentUser), request.Severity), cancellationToken);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AcknowledgeSupportIncidentAsync(
        Guid incidentId, ClaimsPrincipal currentUser, AcknowledgeSupportIncidentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AcknowledgeSupportIncidentCommand(incidentId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReassignSupportIncidentAsync(
        Guid incidentId, ReassignSupportIncidentRequest request, ReassignSupportIncidentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReassignSupportIncidentCommand(incidentId, request.NewAdminUserId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> InvestigateSupportIncidentAsync(
        Guid incidentId, ClaimsPrincipal currentUser, InvestigateSupportIncidentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new InvestigateSupportIncidentCommand(incidentId, CurrentUserId(currentUser)), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResolveSupportIncidentAsync(
        Guid incidentId, ResolveSupportIncidentRequest request, ClaimsPrincipal currentUser, ResolveSupportIncidentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ResolveSupportIncidentCommand(incidentId, CurrentUserId(currentUser), request.Resolution), cancellationToken);

        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CloseSupportIncidentAsync(
        Guid incidentId, CloseSupportIncidentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CloseSupportIncidentCommand(incidentId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReopenSupportIncidentAsync(
        Guid incidentId, ReopenSupportIncidentCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReopenSupportIncidentCommand(incidentId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> MarkIncidentFalsePositiveAsync(
        Guid incidentId, MarkIncidentFalsePositiveCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new MarkIncidentFalsePositiveCommand(incidentId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
