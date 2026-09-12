using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Vehicles;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Vehicles.Commands.ApproveVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RegisterVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RejectVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.RetireVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.SubmitVehicleForVerification;
using SmartTaxi.Application.Fleet.Vehicles.Commands.SuspendVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Commands.UpdateVehicle;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetFleetVehicles;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetOwnerVehicles;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleById;
using SmartTaxi.Application.Fleet.Vehicles.Queries.GetVehicleEligibility;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Vehicles.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class VehicleEndpoints
{
    private const string InvalidEnumError = "Valeur d'énumération inconnue.";

    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var read = app.MapGroup("/api/fleet/vehicles").WithTags("Vehicles").RequireAuthorization();

        read.MapGet("/{vehicleId:guid}", GetByIdAsync)
            .WithName("GetVehicleById")
            .Produces<VehicleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        read.MapGet("/{vehicleId:guid}/eligibility", GetEligibilityAsync)
            .WithName("GetVehicleEligibility")
            .Produces<VehicleEligibilityResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var owner = app.MapGroup("/api/fleet/vehicles")
            .WithTags("Vehicles").RequireAuthorization(Permissions.FleetVehiclesManageOwn);

        owner.MapPost("/", RegisterAsync)
            .WithName("RegisterVehicle")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapGet("/owner/{ownerId:guid}", GetOwnerVehiclesAsync)
            .WithName("GetOwnerVehicles")
            .Produces<IReadOnlyCollection<VehicleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        owner.MapGet("/fleet/{fleetId:guid}", GetFleetVehiclesAsync)
            .WithName("GetFleetVehicles")
            .Produces<IReadOnlyCollection<VehicleResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        owner.MapPut("/{vehicleId:guid}", UpdateAsync)
            .WithName("UpdateVehicle")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{vehicleId:guid}/submit-for-verification", SubmitForVerificationAsync)
            .WithName("SubmitVehicleForVerification")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{vehicleId:guid}/suspend", SuspendAsync)
            .WithName("SuspendVehicle")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        owner.MapPost("/{vehicleId:guid}/retire", RetireAsync)
            .WithName("RetireVehicle")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var review = app.MapGroup("/api/fleet/vehicles")
            .WithTags("Vehicles").RequireAuthorization(Permissions.FleetVehiclesReview);

        review.MapPost("/{vehicleId:guid}/approve", ApproveAsync)
            .WithName("ApproveVehicle")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        review.MapPost("/{vehicleId:guid}/reject", RejectAsync)
            .WithName("RejectVehicle")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<VehicleResponse>, NotFound<ProblemDetails>>> GetByIdAsync(
        Guid vehicleId, GetVehicleByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetVehicleByIdQuery(vehicleId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Véhicule introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(VehicleResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<Ok<VehicleEligibilityResponse>, NotFound<ProblemDetails>>> GetEligibilityAsync(
        Guid vehicleId, GetVehicleEligibilityQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetVehicleEligibilityQuery(vehicleId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Véhicule introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(VehicleEligibilityResponse.FromReport(result.Value!));
    }

    private static async Task<Results<Ok<Guid>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RegisterAsync(
        RegisterVehicleRequest request, ClaimsPrincipal currentUser, RegisterVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FuelType>(request.FuelType, ignoreCase: true, out var fuelType)
            || !Enum.TryParse<TransmissionType>(request.TransmissionType, ignoreCase: true, out var transmissionType)
            || !Enum.TryParse<VehicleCategory>(request.VehicleCategory, ignoreCase: true, out var category))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
        }

        var ownerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new RegisterVehicleCommand(
            ownerId, request.FleetId, request.Brand, request.Model, request.Year, request.Color, request.LicensePlate,
            request.Vin, request.CurrentMileage, fuelType, transmissionType, request.SeatCount, request.HasAirConditioning,
            request.IsAccessible, category);

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

    private static async Task<Results<Ok<IReadOnlyCollection<VehicleResponse>>, ForbidHttpResult>> GetOwnerVehiclesAsync(
        Guid ownerId, ClaimsPrincipal currentUser, GetOwnerVehiclesQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetOwnerVehiclesQuery(userId, ownerId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.Forbid();
        }

        IReadOnlyCollection<VehicleResponse> response = result.Value!.Select(VehicleResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<IReadOnlyCollection<VehicleResponse>>, NotFound<ProblemDetails>>> GetFleetVehiclesAsync(
        Guid fleetId, ClaimsPrincipal currentUser, GetFleetVehiclesQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetFleetVehiclesQuery(userId, fleetId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Flotte introuvable", Detail = result.Error });
        }

        IReadOnlyCollection<VehicleResponse> response = result.Value!.Select(VehicleResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> UpdateAsync(
        Guid vehicleId, UpdateVehicleRequest request, ClaimsPrincipal currentUser,
        UpdateVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<FuelType>(request.FuelType, ignoreCase: true, out var fuelType)
            || !Enum.TryParse<TransmissionType>(request.TransmissionType, ignoreCase: true, out var transmissionType)
            || !Enum.TryParse<VehicleCategory>(request.VehicleCategory, ignoreCase: true, out var category))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidEnumError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new UpdateVehicleCommand(
            userId, vehicleId, request.Brand, request.Model, request.Year, request.Color, request.LicensePlate,
            request.Vin, request.CurrentMileage, fuelType, transmissionType, request.SeatCount, request.HasAirConditioning,
            request.IsAccessible, category);

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

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SubmitForVerificationAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, SubmitVehicleForVerificationCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitVehicleForVerificationCommand(userId, vehicleId), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, SuspendVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendVehicleCommand(userId, vehicleId), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RetireAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, RetireVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RetireVehicleCommand(userId, vehicleId), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid vehicleId, ClaimsPrincipal currentUser, ApproveVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveVehicleCommand(reviewerId, vehicleId), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Forbid(),
                ErrorType.NotFound => TypedResults.NotFound(problem),
                _ => TypedResults.Conflict(problem)
            };
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectAsync(
        Guid vehicleId, RejectVehicleRequest request, ClaimsPrincipal currentUser,
        RejectVehicleCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RejectVehicleCommand(reviewerId, vehicleId, request.Reason), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType switch
            {
                ErrorType.Forbidden => TypedResults.Forbid(),
                ErrorType.NotFound => TypedResults.NotFound(problem),
                ErrorType.Conflict => TypedResults.Conflict(problem),
                _ => TypedResults.BadRequest(problem)
            };
        }

        return TypedResults.NoContent();
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
