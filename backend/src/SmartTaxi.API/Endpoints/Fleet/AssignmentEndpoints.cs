using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Assignments;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Assignments.Commands.ActivateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.ApproveAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CancelAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CompleteAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.CreateAssignment;
using SmartTaxi.Application.Fleet.Assignments.Commands.SuspendAssignment;
using SmartTaxi.Application.Fleet.Assignments.Queries.GetDriverAssignments;
using SmartTaxi.Application.Fleet.Assignments.Queries.GetVehicleAssignments;
using SmartTaxi.Application.Fleet.Assignments.Queries.ValidateAssignmentAvailability;
using SmartTaxi.Application.Identity.Authorization;
using DaysOfWeekFlags = SmartTaxi.Domain.Fleet.Assignments.Enums.DaysOfWeek;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class AssignmentEndpoints
{
    private const string InvalidDayError = "Jour de la semaine inconnu.";

    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fleet/assignments")
            .WithTags("Assignments").RequireAuthorization(Permissions.FleetAssignmentsManageOwn);

        group.MapPost("/", CreateAsync)
            .WithName("CreateAssignment")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/validate-availability", ValidateAvailabilityAsync)
            .WithName("ValidateAssignmentAvailability")
            .Produces<AssignmentAvailabilityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/driver/{driverId:guid}", GetForDriverAsync)
            .WithName("GetDriverAssignments")
            .Produces<IReadOnlyCollection<AssignmentResponse>>(StatusCodes.Status200OK);

        group.MapGet("/vehicle/{vehicleId:guid}", GetForVehicleAsync)
            .WithName("GetVehicleAssignments")
            .Produces<IReadOnlyCollection<AssignmentResponse>>(StatusCodes.Status200OK);

        group.MapPost("/{assignmentId:guid}/approve", ApproveAsync)
            .WithName("ApproveAssignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{assignmentId:guid}/activate", ActivateAsync)
            .WithName("ActivateAssignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{assignmentId:guid}/suspend", SuspendAsync)
            .WithName("SuspendAssignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{assignmentId:guid}/complete", CompleteAsync)
            .WithName("CompleteAssignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{assignmentId:guid}/cancel", CancelAsync)
            .WithName("CancelAssignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static bool TryParseDays(IReadOnlyCollection<string> days, out DaysOfWeekFlags result)
    {
        result = DaysOfWeekFlags.None;

        foreach (var day in days)
        {
            if (!Enum.TryParse<DaysOfWeekFlags>(day, ignoreCase: true, out var parsed))
            {
                return false;
            }

            result |= parsed;
        }

        return true;
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CreateAsync(
        CreateAssignmentRequest request, ClaimsPrincipal currentUser, CreateAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!TryParseDays(request.DaysOfWeek, out var daysOfWeek))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidDayError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateAssignmentCommand(
            userId, request.DriverId, request.VehicleId, request.StartDate, request.EndDate, request.StartTime,
            request.EndTime, daysOfWeek);

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

    private static async Task<Results<Ok<AssignmentAvailabilityResponse>, BadRequest<ProblemDetails>>> ValidateAvailabilityAsync(
        ValidateAssignmentAvailabilityRequest request, ValidateAssignmentAvailabilityQueryHandler handler, CancellationToken cancellationToken)
    {
        if (!TryParseDays(request.DaysOfWeek, out var daysOfWeek))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidDayError });
        }

        var query = new ValidateAssignmentAvailabilityQuery(request.DriverId, request.VehicleId, request.StartDate, request.EndDate, daysOfWeek);
        var report = await handler.Handle(query, cancellationToken);

        return TypedResults.Ok(AssignmentAvailabilityResponse.FromReport(report));
    }

    private static async Task<Ok<IReadOnlyCollection<AssignmentResponse>>> GetForDriverAsync(
        Guid driverId, GetDriverAssignmentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetDriverAssignmentsQuery(driverId), cancellationToken);
        IReadOnlyCollection<AssignmentResponse> response = summaries.Select(AssignmentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<AssignmentResponse>>> GetForVehicleAsync(
        Guid vehicleId, GetVehicleAssignmentsQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetVehicleAssignmentsQuery(vehicleId), cancellationToken);
        IReadOnlyCollection<AssignmentResponse> response = summaries.Select(AssignmentResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid assignmentId, ClaimsPrincipal currentUser, ApproveAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveAssignmentCommand(userId, assignmentId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ActivateAsync(
        Guid assignmentId, ClaimsPrincipal currentUser, ActivateAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ActivateAssignmentCommand(userId, assignmentId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid assignmentId, ClaimsPrincipal currentUser, SuspendAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendAssignmentCommand(userId, assignmentId), cancellationToken);
        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CompleteAsync(
        Guid assignmentId, CompleteAssignmentRequest request, ClaimsPrincipal currentUser,
        CompleteAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CompleteAssignmentCommand(
            userId, assignmentId, request.MileageStart, request.MileageEnd, request.RideCount, request.RevenueGenerated,
            request.IncidentCount);

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

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CancelAsync(
        Guid assignmentId, ClaimsPrincipal currentUser, CancelAssignmentCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new CancelAssignmentCommand(userId, assignmentId), cancellationToken);
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
