using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Rides;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Rides.Commands.AcceptFare;
using SmartTaxi.Application.Rides.Commands.CounterProposeFare;
using SmartTaxi.Application.Rides.Commands.ProposeFare;
using SmartTaxi.Application.Rides.Commands.RejectFare;

namespace SmartTaxi.API.Endpoints.Rides;

/// <summary>
/// Gated by rides.read.own — the master permission list has no dedicated
/// negotiation permission, and the real authorization boundary is each
/// handler's own "only the selected Customer/Driver may act" ownership check.
/// </summary>
public static class RideNegotiationEndpoints
{
    public static IEndpointRouteBuilder MapRideNegotiationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rides").WithTags("Ride Fare Negotiation").RequireAuthorization(Permissions.RidesReadOwn);

        group.MapPost("/{rideId:guid}/fare-proposals", ProposeAsync)
            .WithName("ProposeFare").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{rideId:guid}/fare-proposals/counter", CounterProposeAsync)
            .WithName("CounterProposeFare").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{rideId:guid}/fare-proposals/accept", AcceptAsync)
            .WithName("AcceptFare").Produces<decimal>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{rideId:guid}/fare-proposals/reject", RejectAsync)
            .WithName("RejectFare").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> ProposeAsync(
        Guid rideId, ProposeFareRequest request, ClaimsPrincipal currentUser, ProposeFareCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ProposeFareCommand(CurrentUserId(currentUser), rideId, request.Amount), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CounterProposeAsync(
        Guid rideId, ProposeFareRequest request, ClaimsPrincipal currentUser, CounterProposeFareCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CounterProposeFareCommand(CurrentUserId(currentUser), rideId, request.Amount), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<decimal>, ProblemHttpResult>> AcceptAsync(
        Guid rideId, ClaimsPrincipal currentUser, AcceptFareCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AcceptFareCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectAsync(
        Guid rideId, ClaimsPrincipal currentUser, RejectFareCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectFareCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}
