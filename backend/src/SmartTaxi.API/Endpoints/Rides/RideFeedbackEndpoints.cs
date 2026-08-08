using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using SmartTaxi.API.Contracts.Rides;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Rides.Commands.ActivateSos;
using SmartTaxi.Application.Rides.Commands.CreateRideShareToken;
using SmartTaxi.Application.Rides.Commands.ReportRideMessage;
using SmartTaxi.Application.Rides.Commands.RevokeRideShareToken;
using SmartTaxi.Application.Rides.Commands.SendRideMessage;
using SmartTaxi.Application.Rides.Commands.SubmitRideComplaint;
using SmartTaxi.Application.Rides.Commands.SubmitRideRating;
using SmartTaxi.Application.Rides.Queries.GetPublicRideShareView;
using SmartTaxi.Application.Rides.Queries.GetRideMessages;

namespace SmartTaxi.API.Endpoints.Rides;

/// <summary>Ratings/SOS use their dedicated permissions; complaints, share tokens, and messages fall back to rides.read.own since the master permission list has no dedicated code for them.</summary>
public static class RideFeedbackEndpoints
{
    public static IEndpointRouteBuilder MapRideFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        var ratings = app.MapGroup("/api/rides").WithTags("Ride Ratings").RequireAuthorization(Permissions.RidesRate);

        ratings.MapPost("/{rideId:guid}/ratings", SubmitRatingAsync)
            .WithName("SubmitRideRating").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        var sos = app.MapGroup("/api/rides").WithTags("Ride Safety").RequireAuthorization(Permissions.RidesSos);

        sos.MapPost("/{rideId:guid}/sos", ActivateSosAsync)
            .WithName("ActivateSos").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status403Forbidden);

        var own = app.MapGroup("/api/rides").WithTags("Ride Support").RequireAuthorization(Permissions.RidesReadOwn);

        own.MapPost("/{rideId:guid}/complaints", SubmitComplaintAsync)
            .WithName("SubmitRideComplaint").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status403Forbidden);

        own.MapPost("/{rideId:guid}/share-token", CreateShareTokenAsync)
            .WithName("CreateRideShareToken").Produces<string>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        own.MapDelete("/{rideId:guid}/share-token", RevokeShareTokenAsync)
            .WithName("RevokeRideShareToken").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        own.MapPost("/{rideId:guid}/messages", SendMessageAsync)
            .WithName("SendRideMessage").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        own.MapGet("/{rideId:guid}/messages", GetMessagesAsync)
            .WithName("GetRideMessages").Produces<IReadOnlyCollection<RideMessageResponse>>(StatusCodes.Status200OK);

        own.MapPost("/{rideId:guid}/messages/{messageId:guid}/report", ReportMessageAsync)
            .WithName("ReportRideMessage").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status404NotFound);

        app.MapGet("/api/public/ride-share/{token}", GetPublicShareViewAsync)
            .WithTags("Ride Public Tracking").WithName("GetPublicRideShareView")
            .Produces<PublicRideShareView>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SubmitRatingAsync(
        Guid rideId, SubmitRideRatingRequest request, ClaimsPrincipal currentUser, SubmitRideRatingCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SubmitRideRatingCommand(CurrentUserId(currentUser), rideId, request.Score, request.Comment, request.Tags), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> ActivateSosAsync(
        Guid rideId, ActivateSosRequest request, ClaimsPrincipal currentUser, ActivateSosCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new ActivateSosCommand(CurrentUserId(currentUser), rideId, request.Latitude, request.Longitude, request.Reason),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SubmitComplaintAsync(
        Guid rideId, SubmitRideComplaintRequest request, ClaimsPrincipal currentUser, SubmitRideComplaintCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SubmitRideComplaintCommand(CurrentUserId(currentUser), rideId, request.Category, request.Description), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<string>, ProblemHttpResult>> CreateShareTokenAsync(
        Guid rideId, ClaimsPrincipal currentUser, CreateRideShareTokenCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateRideShareTokenCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeShareTokenAsync(
        Guid rideId, ClaimsPrincipal currentUser, RevokeRideShareTokenCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RevokeRideShareTokenCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SendMessageAsync(
        Guid rideId, SendRideMessageRequest request, ClaimsPrincipal currentUser, SendRideMessageCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SendRideMessageCommand(CurrentUserId(currentUser), rideId, request.MessageType, request.Content), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideMessageResponse>>, ProblemHttpResult>> GetMessagesAsync(
        Guid rideId, ClaimsPrincipal currentUser, GetRideMessagesQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideMessagesQuery(CurrentUserId(currentUser), rideId), cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ToProblem();
        }

        IReadOnlyCollection<RideMessageResponse> response = result.Value!.Select(RideMessageResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReportMessageAsync(
        Guid rideId, Guid messageId, ClaimsPrincipal currentUser, ReportRideMessageCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReportRideMessageCommand(CurrentUserId(currentUser), rideId, messageId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<PublicRideShareView>, ProblemHttpResult>> GetPublicShareViewAsync(
        string token, GetPublicRideShareViewQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetPublicRideShareViewQuery(token), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }
}
