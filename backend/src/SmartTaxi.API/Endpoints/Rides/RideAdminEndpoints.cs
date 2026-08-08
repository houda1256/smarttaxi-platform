using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Rides;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Rides.Commands.AcknowledgeSos;
using SmartTaxi.Application.Rides.Commands.CancelRideByAdmin;
using SmartTaxi.Application.Rides.Commands.DismissRideComplaint;
using SmartTaxi.Application.Rides.Commands.ResolveRideComplaint;
using SmartTaxi.Application.Rides.Commands.ResolveSos;
using SmartTaxi.Application.Rides.Queries.GetActiveRidesAdmin;
using SmartTaxi.Application.Rides.Queries.GetAdminRides;
using SmartTaxi.Application.Rides.Queries.GetRideByIdAdmin;
using SmartTaxi.Application.Rides.Queries.GetRideComplaints;
using SmartTaxi.Application.Rides.Queries.GetRideRatings;
using SmartTaxi.Application.Rides.Queries.GetRideSafetyEvents;
using SmartTaxi.Domain.Rides.Enums;

namespace SmartTaxi.API.Endpoints.Rides;

public static class RideAdminEndpoints
{
    public static IEndpointRouteBuilder MapRideAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var monitor = app.MapGroup("/api/admin/rides").WithTags("Ride Administration").RequireAuthorization(Permissions.RidesMonitor);

        monitor.MapGet("/", GetAllAsync).WithName("GetAdminRides").Produces<IReadOnlyCollection<RideResponse>>(StatusCodes.Status200OK);

        monitor.MapGet("/active", GetActiveAsync)
            .WithName("GetActiveRidesAdmin").Produces<IReadOnlyCollection<RideResponse>>(StatusCodes.Status200OK);

        monitor.MapGet("/{rideId:guid}", GetByIdAsync)
            .WithName("GetRideByIdAdmin").Produces<RideResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        monitor.MapGet("/{rideId:guid}/ratings", GetRatingsAsync)
            .WithName("GetRideRatingsAdmin").Produces<IReadOnlyCollection<RideRatingResponse>>(StatusCodes.Status200OK);

        monitor.MapGet("/{rideId:guid}/safety-events", GetSafetyEventsAsync)
            .WithName("GetRideSafetyEventsAdmin").Produces<IReadOnlyCollection<RideSafetyEventResponse>>(StatusCodes.Status200OK);

        monitor.MapPost("/sos/{safetyEventId:guid}/acknowledge", AcknowledgeSosAsync)
            .WithName("AcknowledgeSos").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        monitor.MapPost("/sos/{safetyEventId:guid}/resolve", ResolveSosAsync)
            .WithName("ResolveSos").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var cancel = app.MapGroup("/api/admin/rides").WithTags("Ride Administration").RequireAuthorization(Permissions.RidesCancelAdmin);

        cancel.MapPost("/{rideId:guid}/cancel", CancelAsync)
            .WithName("CancelRideByAdmin").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var dispute = app.MapGroup("/api/admin/rides").WithTags("Ride Administration").RequireAuthorization(Permissions.RidesDispute);

        dispute.MapGet("/{rideId:guid}/complaints", GetComplaintsAsync)
            .WithName("GetRideComplaintsAdmin").Produces<IReadOnlyCollection<RideComplaintResponse>>(StatusCodes.Status200OK);

        dispute.MapPost("/complaints/{complaintId:guid}/resolve", ResolveComplaintAsync)
            .WithName("ResolveRideComplaint").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        dispute.MapPost("/complaints/{complaintId:guid}/dismiss", DismissComplaintAsync)
            .WithName("DismissRideComplaint").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Ok<IReadOnlyCollection<RideResponse>>> GetAllAsync(
        RideStatus? status, GetAdminRidesQueryHandler handler, CancellationToken cancellationToken)
    {
        var rides = await handler.Handle(new GetAdminRidesQuery(status), cancellationToken);
        IReadOnlyCollection<RideResponse> response = rides.Select(RideResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<RideResponse>>> GetActiveAsync(
        GetActiveRidesAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var rides = await handler.Handle(new GetActiveRidesAdminQuery(), cancellationToken);
        IReadOnlyCollection<RideResponse> response = rides.Select(RideResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<RideResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid rideId, GetRideByIdAdminQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideByIdAdminQuery(rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(RideResponse.FromSummary(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideRatingResponse>>, ProblemHttpResult>> GetRatingsAsync(
        Guid rideId, GetRideRatingsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideRatingsQuery(rideId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<RideRatingResponse> response = result.Value.Select(RideRatingResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideSafetyEventResponse>>, ProblemHttpResult>> GetSafetyEventsAsync(
        Guid rideId, GetRideSafetyEventsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideSafetyEventsQuery(rideId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<RideSafetyEventResponse> response = result.Value.Select(RideSafetyEventResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AcknowledgeSosAsync(
        Guid safetyEventId, AcknowledgeSosCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AcknowledgeSosCommand(safetyEventId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResolveSosAsync(
        Guid safetyEventId, ResolveSosCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResolveSosCommand(safetyEventId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelAsync(
        Guid rideId, CancelRideByAdminRequest request, ClaimsPrincipal currentUser, CancelRideByAdminCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CancelRideByAdminCommand(CurrentUserId(currentUser), rideId, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideComplaintResponse>>, ProblemHttpResult>> GetComplaintsAsync(
        Guid rideId, GetRideComplaintsQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideComplaintsQuery(rideId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<RideComplaintResponse> response = result.Value.Select(RideComplaintResponse.FromEntity).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResolveComplaintAsync(
        Guid complaintId, ResolveComplaintRequest request, ResolveRideComplaintCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ResolveRideComplaintCommand(complaintId, request.Resolution), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DismissComplaintAsync(
        Guid complaintId, ResolveComplaintRequest request, DismissRideComplaintCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DismissRideComplaintCommand(complaintId, request.Resolution), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
