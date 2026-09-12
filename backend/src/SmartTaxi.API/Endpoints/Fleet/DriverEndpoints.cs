using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartTaxi.API.Contracts.Fleet.Drivers;
using SmartTaxi.Application.Common;
using SmartTaxi.Application.Fleet.Drivers.Commands.ApproveDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.CreateDriverProfile;
using SmartTaxi.Application.Fleet.Drivers.Commands.RejectDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.SetDriverAvailability;
using SmartTaxi.Application.Fleet.Drivers.Commands.SubmitDriverForReview;
using SmartTaxi.Application.Fleet.Drivers.Commands.SuspendDriver;
using SmartTaxi.Application.Fleet.Drivers.Commands.UpdateDriverProfile;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetDriverProfileById;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetEligibleDrivers;
using SmartTaxi.Application.Fleet.Drivers.Queries.GetMyDriverProfile;
using SmartTaxi.Application.Identity.Authorization;
using SmartTaxi.Domain.Fleet.Drivers.Enums;

namespace SmartTaxi.API.Endpoints.Fleet;

public static class DriverEndpoints
{
    private const string InvalidStatusError = "Statut de disponibilité inconnu.";

    public static IEndpointRouteBuilder MapDriverEndpoints(this IEndpointRouteBuilder app)
    {
        var self = app.MapGroup("/api/fleet/drivers")
            .WithTags("Drivers").RequireAuthorization(Permissions.FleetDriversManageOwn);

        self.MapPost("/", CreateAsync)
            .WithName("CreateDriverProfile")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        self.MapGet("/me", GetMineAsync)
            .WithName("GetMyDriverProfile")
            .Produces<DriverProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        self.MapGet("/eligible", GetEligibleAsync)
            .WithName("GetEligibleDrivers")
            .Produces<IReadOnlyCollection<DriverProfileResponse>>(StatusCodes.Status200OK);

        self.MapGet("/{driverProfileId:guid}", GetByIdAsync)
            .WithName("GetDriverProfileById")
            .Produces<DriverProfileResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        self.MapPut("/{driverProfileId:guid}", UpdateAsync)
            .WithName("UpdateDriverProfile")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        self.MapPost("/{driverProfileId:guid}/submit-for-review", SubmitForReviewAsync)
            .WithName("SubmitDriverForReview")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        self.MapPost("/{driverProfileId:guid}/availability", SetAvailabilityAsync)
            .WithName("SetDriverAvailability")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var review = app.MapGroup("/api/fleet/drivers")
            .WithTags("Drivers").RequireAuthorization(Permissions.FleetDriversReview);

        review.MapPost("/{driverProfileId:guid}/approve", ApproveAsync)
            .WithName("ApproveDriver")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        review.MapPost("/{driverProfileId:guid}/reject", RejectAsync)
            .WithName("RejectDriver")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        review.MapPost("/{driverProfileId:guid}/suspend", SuspendAsync)
            .WithName("SuspendDriver")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<Guid>, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> CreateAsync(
        CreateDriverProfileRequest request, ClaimsPrincipal currentUser,
        CreateDriverProfileCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new CreateDriverProfileCommand(
            userId, request.DriverLicenseNumber, request.DriverLicenseExpiration, request.TaxiLicenseNumber, request.IndependentDriver);

        var result = await handler.Handle(command, cancellationToken);

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

        return TypedResults.Ok(result.Value);
    }

    private static async Task<Results<Ok<DriverProfileResponse>, NotFound<ProblemDetails>>> GetMineAsync(
        ClaimsPrincipal currentUser, GetMyDriverProfileQueryHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new GetMyDriverProfileQuery(userId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Profil introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(DriverProfileResponse.FromSummary(result.Value!));
    }

    private static async Task<Ok<IReadOnlyCollection<DriverProfileResponse>>> GetEligibleAsync(
        GetEligibleDriversQueryHandler handler, CancellationToken cancellationToken)
    {
        var summaries = await handler.Handle(new GetEligibleDriversQuery(), cancellationToken);
        IReadOnlyCollection<DriverProfileResponse> response = summaries.Select(DriverProfileResponse.FromSummary).ToList();
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<DriverProfileResponse>, NotFound<ProblemDetails>>> GetByIdAsync(
        Guid driverProfileId, GetDriverProfileByIdQueryHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetDriverProfileByIdQuery(driverProfileId), cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Profil introuvable", Detail = result.Error });
        }

        return TypedResults.Ok(DriverProfileResponse.FromSummary(result.Value!));
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> UpdateAsync(
        Guid driverProfileId, UpdateDriverProfileRequest request, ClaimsPrincipal currentUser,
        UpdateDriverProfileCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var command = new UpdateDriverProfileCommand(
            userId, driverProfileId, request.DriverLicenseNumber, request.DriverLicenseExpiration, request.TaxiLicenseNumber);

        var result = await handler.Handle(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return TypedResults.NotFound(new ProblemDetails { Title = "Profil introuvable", Detail = result.Error });
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SubmitForReviewAsync(
        Guid driverProfileId, ClaimsPrincipal currentUser, SubmitDriverForReviewCommandHandler handler, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SubmitDriverForReviewCommand(userId, driverProfileId), cancellationToken);

        return ToNoContentResult(result);
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> SetAvailabilityAsync(
        Guid driverProfileId, SetDriverAvailabilityRequest request, ClaimsPrincipal currentUser,
        SetDriverAvailabilityCommandHandler handler, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<DriverAvailabilityStatus>(request.Status, ignoreCase: true, out var status))
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Requête invalide", Detail = InvalidStatusError });
        }

        var userId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SetDriverAvailabilityCommand(userId, driverProfileId, status), cancellationToken);

        if (!result.IsSuccess)
        {
            var problem = new ProblemDetails { Title = "Requête invalide", Detail = result.Error };
            return result.ErrorType == ErrorType.NotFound ? TypedResults.NotFound(problem) : TypedResults.BadRequest(problem);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> ApproveAsync(
        Guid driverProfileId, ClaimsPrincipal currentUser, ApproveDriverCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new ApproveDriverCommand(reviewerId, driverProfileId), cancellationToken);

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

    private static async Task<Results<NoContent, BadRequest<ProblemDetails>, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> RejectAsync(
        Guid driverProfileId, RejectDriverRequest request, ClaimsPrincipal currentUser,
        RejectDriverCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new RejectDriverCommand(reviewerId, driverProfileId, request.Reason), cancellationToken);

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

    private static async Task<Results<NoContent, ForbidHttpResult, NotFound<ProblemDetails>, Conflict<ProblemDetails>>> SuspendAsync(
        Guid driverProfileId, ClaimsPrincipal currentUser, SuspendDriverCommandHandler handler, CancellationToken cancellationToken)
    {
        var reviewerId = Guid.Parse(currentUser.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var result = await handler.Handle(new SuspendDriverCommand(reviewerId, driverProfileId), cancellationToken);

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
