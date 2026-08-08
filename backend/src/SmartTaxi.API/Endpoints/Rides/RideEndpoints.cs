using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Rides;
using SmartTaxi.Application.Fleet.Drivers.Abstractions;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Application.Rides;
using SmartTaxi.Application.Rides.Commands.CancelRideByCustomer;
using SmartTaxi.Application.Rides.Commands.CancelRideByDriver;
using SmartTaxi.Application.Rides.Commands.CompleteRide;
using SmartTaxi.Application.Rides.Commands.CreateRide;
using SmartTaxi.Application.Rides.Commands.DriverAcceptRide;
using SmartTaxi.Application.Rides.Commands.DriverArrived;
using SmartTaxi.Application.Rides.Commands.DriverEnRoute;
using SmartTaxi.Application.Rides.Commands.DriverRejectRide;
using SmartTaxi.Application.Rides.Commands.PassengerOnBoard;
using SmartTaxi.Application.Rides.Commands.ReportCustomerNoShow;
using SmartTaxi.Application.Rides.Commands.ReportDriverNoShow;
using SmartTaxi.Application.Rides.Commands.SearchDrivers;
using SmartTaxi.Application.Rides.Commands.SelectDriver;
using SmartTaxi.Application.Rides.Commands.StartRide;
using SmartTaxi.Application.Rides.Commands.UpdateDriverAvailabilityLocation;
using SmartTaxi.Application.Rides.Commands.UpdateDriverLocation;
using SmartTaxi.Application.Rides.Queries.GetMyRidesForCustomer;
using SmartTaxi.Application.Rides.Queries.GetMyRidesForDriver;
using SmartTaxi.Application.Rides.Queries.GetPendingRideRequestsForDriver;
using SmartTaxi.Application.Rides.Queries.GetRecommendedDrivers;
using SmartTaxi.Application.Rides.Queries.GetRideById;

namespace SmartTaxi.API.Endpoints.Rides;

/// <summary>
/// Route convention note: the repository has no existing /api/v1 prefix
/// anywhere (Identity/Fleet both use flat /api/{module} routes), so Ride
/// follows the same /api/rides convention rather than introducing /api/v1
/// unilaterally for one module — documented mismatch against the master
/// prompt's illustrative /api/v1 examples.
/// </summary>
public static class RideEndpoints
{
    public static IEndpointRouteBuilder MapRideEndpoints(this IEndpointRouteBuilder app)
    {
        var customer = app.MapGroup("/api/rides").WithTags("Rides");

        customer.MapPost("/", CreateAsync).RequireAuthorization(Permissions.RidesCreate)
            .WithName("CreateRide").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status400BadRequest);

        customer.MapGet("/mine", GetMineForCustomerAsync).RequireAuthorization(Permissions.RidesReadOwn)
            .WithName("GetMyRidesAsCustomer").Produces<IReadOnlyCollection<RideResponse>>(StatusCodes.Status200OK);

        customer.MapGet("/{rideId:guid}", GetByIdAsync).RequireAuthorization(Permissions.RidesReadOwn)
            .WithName("GetRideById").Produces<RideResponse>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        customer.MapPost("/{rideId:guid}/search-drivers", SearchDriversAsync).RequireAuthorization(Permissions.RidesSearchDrivers)
            .WithName("SearchDrivers").Produces<int>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status404NotFound);

        customer.MapGet("/{rideId:guid}/recommended-drivers", GetRecommendedDriversAsync).RequireAuthorization(Permissions.RidesSearchDrivers)
            .WithName("GetRecommendedDrivers").Produces<IReadOnlyCollection<RideDriverRecommendationSummary>>(StatusCodes.Status200OK);

        customer.MapPost("/{rideId:guid}/select-driver", SelectDriverAsync).RequireAuthorization(Permissions.RidesSelectDriver)
            .WithName("SelectDriver").Produces<Guid>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        customer.MapPost("/{rideId:guid}/cancel", CancelByCustomerAsync).RequireAuthorization(Permissions.RidesCancelOwn)
            .WithName("CancelRideByCustomer").Produces<decimal>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        customer.MapPost("/{rideId:guid}/report-driver-no-show", ReportDriverNoShowAsync).RequireAuthorization(Permissions.RidesReadOwn)
            .WithName("ReportDriverNoShow").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        var driver = app.MapGroup("/api/rides/driver").WithTags("Rides");

        driver.MapGet("/mine", GetMineForDriverAsync).RequireAuthorization(Permissions.RidesReadOwn)
            .WithName("GetMyRidesAsDriver").Produces<IReadOnlyCollection<RideResponse>>(StatusCodes.Status200OK);

        driver.MapGet("/pending-requests", GetPendingRequestsAsync).RequireAuthorization(Permissions.RidesAccept)
            .WithName("GetPendingRideRequestsForDriver").Produces<IReadOnlyCollection<RideResponse>>(StatusCodes.Status200OK);

        driver.MapPost("/location", UpdateAvailabilityLocationAsync).RequireAuthorization(Permissions.RidesUpdateLocation)
            .WithName("UpdateDriverAvailabilityLocation").Produces(StatusCodes.Status204NoContent);

        var rideDriver = app.MapGroup("/api/rides").WithTags("Rides");

        rideDriver.MapPost("/{rideId:guid}/accept", AcceptAsync).RequireAuthorization(Permissions.RidesAccept)
            .WithName("DriverAcceptRide").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/reject", RejectAsync).RequireAuthorization(Permissions.RidesReject)
            .WithName("DriverRejectRide").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/en-route", EnRouteAsync).RequireAuthorization(Permissions.RidesAccept)
            .WithName("DriverEnRoute").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/arrived", ArrivedAsync).RequireAuthorization(Permissions.RidesAccept)
            .WithName("DriverArrived").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/passenger-onboard", PassengerOnBoardAsync).RequireAuthorization(Permissions.RidesStart)
            .WithName("PassengerOnBoard").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/start", StartAsync).RequireAuthorization(Permissions.RidesStart)
            .WithName("StartRide").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/complete", CompleteAsync).RequireAuthorization(Permissions.RidesComplete)
            .WithName("CompleteRide").Produces<decimal>(StatusCodes.Status200OK).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/cancel-by-driver", CancelByDriverAsync).RequireAuthorization(Permissions.RidesCancelOwn)
            .WithName("CancelRideByDriver").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/report-customer-no-show", ReportCustomerNoShowAsync).RequireAuthorization(Permissions.RidesAccept)
            .WithName("ReportCustomerNoShow").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        rideDriver.MapPost("/{rideId:guid}/location", UpdateLocationAsync).RequireAuthorization(Permissions.RidesUpdateLocation)
            .WithName("UpdateDriverLocation").Produces(StatusCodes.Status204NoContent).ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static Guid CurrentUserId(ClaimsPrincipal currentUser) => Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> CreateAsync(
        CreateRideRequest request, ClaimsPrincipal currentUser, CreateRideCommandHandler handler, CancellationToken cancellationToken)
    {
        var command = new CreateRideCommand(
            CurrentUserId(currentUser), request.RideType, request.PickupAddress, request.PickupLatitude, request.PickupLongitude,
            request.DestinationAddress, request.DestinationLatitude, request.DestinationLongitude, request.ScheduledAt,
            request.PassengerCount, request.LuggageCount, request.NeedsAirConditioning, request.NeedsAccessibleVehicle,
            request.HasChildSeatRequest, request.HasPet, request.PreferredVehicleCategory, request.PreferredPaymentMethod,
            request.SpecialInstructions);

        var result = await handler.Handle(command, cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Ok<IReadOnlyCollection<RideResponse>>> GetMineForCustomerAsync(
        ClaimsPrincipal currentUser, GetMyRidesForCustomerQueryHandler handler, CancellationToken cancellationToken)
    {
        var rides = await handler.Handle(new GetMyRidesForCustomerQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<RideResponse> response = rides.Select(RideResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideResponse>>, ProblemHttpResult>> GetMineForDriverAsync(
        ClaimsPrincipal currentUser, IDriverProfileRepository driverRepository, GetMyRidesForDriverQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var driver = await driverRepository.GetByUserIdAsync(CurrentUserId(currentUser), cancellationToken);

        if (driver is null)
        {
            return TypedResults.Problem(detail: "Profil chauffeur introuvable.", statusCode: StatusCodes.Status404NotFound);
        }

        var rides = await handler.Handle(new GetMyRidesForDriverQuery(driver.Id), cancellationToken);
        IReadOnlyCollection<RideResponse> response = rides.Select(RideResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Ok<IReadOnlyCollection<RideResponse>>> GetPendingRequestsAsync(
        ClaimsPrincipal currentUser, GetPendingRideRequestsForDriverQueryHandler handler, CancellationToken cancellationToken)
    {
        var rides = await handler.Handle(new GetPendingRideRequestsForDriverQuery(CurrentUserId(currentUser)), cancellationToken);
        IReadOnlyCollection<RideResponse> response = rides.Select(RideResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<RideResponse>, ProblemHttpResult>> GetByIdAsync(
        Guid rideId, ClaimsPrincipal currentUser, GetRideByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRideByIdQuery(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(RideResponse.FromSummary(result.Value!)) : result.ToProblem();
    }

    private static async Task<Results<Ok<int>, ProblemHttpResult>> SearchDriversAsync(
        Guid rideId, ClaimsPrincipal currentUser, SearchDriversCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SearchDriversCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<IReadOnlyCollection<RideDriverRecommendationSummary>>, ProblemHttpResult>> GetRecommendedDriversAsync(
        Guid rideId, ClaimsPrincipal currentUser, GetRecommendedDriversQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetRecommendedDriversQuery(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<Guid>, ProblemHttpResult>> SelectDriverAsync(
        Guid rideId, SelectDriverRequest request, ClaimsPrincipal currentUser, SelectDriverCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new SelectDriverCommand(CurrentUserId(currentUser), rideId, request.DriverId, request.VehicleId), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> AcceptAsync(
        Guid rideId, ClaimsPrincipal currentUser, DriverAcceptRideCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DriverAcceptRideCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RejectAsync(
        Guid rideId, DriverRejectRideRequest request, ClaimsPrincipal currentUser, DriverRejectRideCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DriverRejectRideCommand(CurrentUserId(currentUser), rideId, request.Reason), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> EnRouteAsync(
        Guid rideId, ClaimsPrincipal currentUser, DriverEnRouteCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DriverEnRouteCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ArrivedAsync(
        Guid rideId, ClaimsPrincipal currentUser, DriverArrivedCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DriverArrivedCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> PassengerOnBoardAsync(
        Guid rideId, ClaimsPrincipal currentUser, PassengerOnBoardCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new PassengerOnBoardCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> StartAsync(
        Guid rideId, ClaimsPrincipal currentUser, StartRideCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new StartRideCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<Ok<decimal>, ProblemHttpResult>> CompleteAsync(
        Guid rideId, CompleteRideActualsRequest request, ClaimsPrincipal currentUser, CompleteRideCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CompleteRideCommand(CurrentUserId(currentUser), rideId, request.ActualDistanceKm, request.ActualDurationMinutes),
            cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<Ok<decimal>, ProblemHttpResult>> CancelByCustomerAsync(
        Guid rideId, CancelRideByCustomerRequest request, ClaimsPrincipal currentUser, CancelRideByCustomerCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CancelRideByCustomerCommand(CurrentUserId(currentUser), rideId, request.Reason, request.Details), cancellationToken);
        return result.IsSuccess ? TypedResults.Ok(result.Value) : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> CancelByDriverAsync(
        Guid rideId, CancelRideByDriverRequest request, ClaimsPrincipal currentUser, CancelRideByDriverCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new CancelRideByDriverCommand(CurrentUserId(currentUser), rideId, request.Reason, request.Details), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReportCustomerNoShowAsync(
        Guid rideId, ClaimsPrincipal currentUser, ReportCustomerNoShowCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReportCustomerNoShowCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ReportDriverNoShowAsync(
        Guid rideId, ClaimsPrincipal currentUser, ReportDriverNoShowCommandHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ReportDriverNoShowCommand(CurrentUserId(currentUser), rideId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateLocationAsync(
        Guid rideId, UpdateDriverLocationRequest request, ClaimsPrincipal currentUser, UpdateDriverLocationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateDriverLocationCommand(CurrentUserId(currentUser), rideId, request.Latitude, request.Longitude, request.Speed,
                request.Heading, request.Accuracy),
            cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> UpdateAvailabilityLocationAsync(
        UpdateDriverAvailabilityLocationRequest request, ClaimsPrincipal currentUser, UpdateDriverAvailabilityLocationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateDriverAvailabilityLocationCommand(CurrentUserId(currentUser), request.Latitude, request.Longitude), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : result.ToProblem();
    }
}

public sealed record CompleteRideActualsRequest(decimal ActualDistanceKm, int ActualDurationMinutes);
