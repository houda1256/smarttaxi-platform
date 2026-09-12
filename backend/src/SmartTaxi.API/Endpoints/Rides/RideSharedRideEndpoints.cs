using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Rides;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Rides.Commands.ApproveSharedRideByDriver;
using SmartTaxi.Application.Rides.Commands.CustomerApproveSharedRide;
using SmartTaxi.Application.Rides.Commands.CustomerRejectSharedRide;
using SmartTaxi.Application.Rides.Commands.FindSharedRideMatch;
using SmartTaxi.Application.Rides.Commands.RejectSharedRideByDriver;
using SmartTaxi.Application.Rides.Queries.GetSharedRideFareBreakdown;

namespace SmartTaxi.API.Endpoints.Rides;

public static class RideSharedRideEndpoints
{
    public static IEndpointRouteBuilder MapRideSharedRideEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rides").WithTags("Shared Rides").RequireAuthorization(Permissions.RidesSharedManage);

        group.MapPost("/{rideId:guid}/shared-match/find", FindMatchAsync)
            .WithName("FindSharedRideMatch").Produces<Guid?>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/shared-matches/{matchId:guid}/customer-approve", CustomerApproveAsync)
            .WithName("CustomerApproveSharedRide").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/shared-matches/{matchId:guid}/customer-reject", CustomerRejectAsync)
            .WithName("CustomerRejectSharedRide").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/shared-matches/{matchId:guid}/driver-approve", DriverApproveAsync)
            .WithName("ApproveSharedRideByDriver").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/shared-matches/{matchId:guid}/driver-reject", DriverRejectAsync)
            .WithName("RejectSharedRideByDriver").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/shared-matches/{matchId:guid}/fare-breakdown", GetFareBreakdownAsync)
            .WithName("GetSharedRideFareBreakdown").Produces<IReadOnlyCollection<SharedRideFareShare>>(StatusCodes.Status200OK);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid?>, ProblemHttpResult>> FindMatchAsync(
        Guid rideId, ClaimsPrincipal currentUser, FindSharedRideMatchCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new FindSharedRideMatchCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CustomerApproveAsync(
        Guid matchId, ClaimsPrincipal currentUser, CustomerApproveSharedRideCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CustomerApproveSharedRideCommand(CurrentUserId(currentUser), matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CustomerRejectAsync(
        Guid matchId, ClaimsPrincipal currentUser, CustomerRejectSharedRideCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CustomerRejectSharedRideCommand(CurrentUserId(currentUser), matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DriverApproveAsync(
        Guid matchId, ApproveSharedRideByDriverRequest request, ClaimsPrincipal currentUser,
        ApproveSharedRideByDriverCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ApproveSharedRideByDriverCommand(CurrentUserId(currentUser), matchId, request.VehicleId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> DriverRejectAsync(
        Guid matchId, ClaimsPrincipal currentUser, RejectSharedRideByDriverCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RejectSharedRideByDriverCommand(CurrentUserId(currentUser), matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<SharedRideFareShare>>, ProblemHttpResult>> GetFareBreakdownAsync(
        Guid matchId, GetSharedRideFareBreakdownQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetSharedRideFareBreakdownQuery(matchId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
